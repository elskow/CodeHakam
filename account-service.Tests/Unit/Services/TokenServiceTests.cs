using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AccountService.Configuration;
using AccountService.Data;
using AccountService.Models;
using AccountService.Services.Impl;
using AccountService.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AccountService.Tests.Unit.Services;

public class TokenServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<TokenService>> _loggerMock;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"TokenServiceTests_{Guid.NewGuid()}");
        _loggerMock = new Mock<ILogger<TokenService>>();

        var settings = new JwtSettings
        {
            SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345678901234567890",
            Issuer = "CodeHakam.Test",
            Audience = "CodeHakam.Test.Audience",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        };
        _jwtSettings = Options.Create(settings);

        _tokenService = new TokenService(_jwtSettings, _context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_WithValidUser_ShouldGenerateValidJwt()
    {
        var user = TestDataBuilder.CreateTestUser(
            username: "testuser",
            email: "test@example.com",
            rating: 1800);
        user.Id = 1;
        var roles = new List<string> { "user", "moderator" };

        var token = await _tokenService.GenerateAccessTokenAsync(user, roles);

        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("CodeHakam.Test");
        jwtToken.Audiences.Should().Contain("CodeHakam.Test.Audience");
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldIncludeUserClaims()
    {
        var user = TestDataBuilder.CreateTestUser(
            username: "johndoe",
            email: "john@example.com",
            rating: 2000,
            isVerified: true,
            isBanned: false);
        user.Id = 123;
        var roles = new List<string> { "user" };

        var token = await _tokenService.GenerateAccessTokenAsync(user, roles);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "123");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "john@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == "username" && c.Value == "johndoe");
        jwtToken.Claims.Should().Contain(c => c.Type == "rating" && c.Value == "2000");
        jwtToken.Claims.Should().Contain(c => c.Type == "is_verified" && c.Value == "True");
        jwtToken.Claims.Should().Contain(c => c.Type == "is_banned" && c.Value == "False");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldIncludeRoleClaims()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.Id = 1;
        var roles = new List<string> { "user", "moderator", "admin" };

        var token = await _tokenService.GenerateAccessTokenAsync(user, roles);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().HaveCount(3);
        roleClaims.Should().Contain("user");
        roleClaims.Should().Contain("moderator");
        roleClaims.Should().Contain("admin");
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_WithNoRoles_ShouldGenerateTokenWithoutRoleClaims()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.Id = 1;
        var roles = new List<string>();

        var token = await _tokenService.GenerateAccessTokenAsync(user, roles);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldGenerateBase64Token()
    {
        var token = await _tokenService.GenerateRefreshTokenAsync();

        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(50);

        var isBase64 = IsBase64String(token);
        isBase64.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldGenerateUniqueTokens()
    {
        var token1 = await _tokenService.GenerateRefreshTokenAsync();
        var token2 = await _tokenService.GenerateRefreshTokenAsync();
        var token3 = await _tokenService.GenerateRefreshTokenAsync();

        token1.Should().NotBe(token2);
        token2.Should().NotBe(token3);
        token1.Should().NotBe(token3);
    }

    [Fact]
    public async Task GenerateEmailVerificationTokenAsync_ShouldGenerateUrlSafeToken()
    {
        var token = await _tokenService.GenerateEmailVerificationTokenAsync();

        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_ShouldGenerateUrlSafeToken()
    {
        var token = await _tokenService.GeneratePasswordResetTokenAsync();

        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void HashToken_WithSameInput_ShouldProduceSameHash()
    {
        var token = "sample_refresh_token_12345";

        var hash1 = _tokenService.HashToken(token);
        var hash2 = _tokenService.HashToken(token);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_WithDifferentInputs_ShouldProduceDifferentHashes()
    {
        var token1 = "token_abc_123";
        var token2 = "token_xyz_789";

        var hash1 = _tokenService.HashToken(token1);
        var hash2 = _tokenService.HashToken(token2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_ShouldProduceBase64Hash()
    {
        var token = "sample_token";

        var hash = _tokenService.HashToken(token);

        hash.Should().NotBeNullOrEmpty();
        var isBase64 = IsBase64String(hash);
        isBase64.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidActiveToken_ShouldReturnTrue()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "valid_refresh_token_123";
        var tokenHash = _tokenService.HashToken(rawToken);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var result = await _tokenService.ValidateRefreshTokenAsync(rawToken, user.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "expired_token";
        var tokenHash = _tokenService.HashToken(rawToken);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var result = await _tokenService.ValidateRefreshTokenAsync(rawToken, user.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithRevokedToken_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "revoked_token";
        var tokenHash = _tokenService.HashToken(rawToken);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var result = await _tokenService.ValidateRefreshTokenAsync(rawToken, user.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithNonExistentToken_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _tokenService.ValidateRefreshTokenAsync("non_existent_token", user.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithWrongUserId_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "token_for_user_1";
        var tokenHash = _tokenService.HashToken(rawToken);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var result = await _tokenService.ValidateRefreshTokenAsync(rawToken, 999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithActiveToken_ShouldRevokeSuccessfully()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "token_to_revoke";
        var tokenHash = _tokenService.HashToken(rawToken);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        await _tokenService.RevokeRefreshTokenAsync(rawToken, "192.168.1.1");

        var revokedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        revokedToken.Should().NotBeNull();
        revokedToken!.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        revokedToken.RevokedByIp.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithAlreadyRevokedToken_ShouldNotChangeIt()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "already_revoked_token";
        var tokenHash = _tokenService.HashToken(rawToken);
        var originalRevokedAt = DateTime.UtcNow.AddDays(-1);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            RevokedAt = originalRevokedAt,
            RevokedByIp = "10.0.0.1",
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        await _tokenService.RevokeRefreshTokenAsync(rawToken, "192.168.1.1");

        var token = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        token!.RevokedAt.Should().Be(originalRevokedAt);
        token.RevokedByIp.Should().Be("10.0.0.1");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonExistentToken_ShouldNotThrow()
    {
        var act = async () => await _tokenService.RevokeRefreshTokenAsync("non_existent_token");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_ShouldRevokeAllActiveTokens()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token1 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken("token1"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };
        var token2 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken("token2"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };
        var token3 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken("token3"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddDays(-1),
            CreatedByIp = "127.0.0.1"
        };

        await _context.RefreshTokens.AddRangeAsync(token1, token2, token3);
        await _context.SaveChangesAsync();

        await _tokenService.RevokeAllUserRefreshTokensAsync(user.Id);

        var allTokens = await _context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        allTokens.Should().HaveCount(3);
        allTokens.Where(t => t.RevokedAt != null).Should().HaveCount(3);
    }

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_WithNoTokens_ShouldNotThrow()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var act = async () => await _tokenService.RevokeAllUserRefreshTokensAsync(user.Id);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WithValidExpiredToken_ShouldReturnPrincipal()
    {
        var user = TestDataBuilder.CreateTestUser(username: "testuser", email: "test@example.com");
        user.Id = 1;
        var roles = new List<string> { "user" };

        var expiredSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.Value.SecretKey,
            Issuer = _jwtSettings.Value.Issuer,
            Audience = _jwtSettings.Value.Audience,
            ExpiryMinutes = -5
        };
        var expiredTokenService = new TokenService(Options.Create(expiredSettings), _context, _loggerMock.Object);

        var expiredToken = await expiredTokenService.GenerateAccessTokenAsync(user, roles);

        var principal = _tokenService.GetPrincipalFromExpiredToken(expiredToken);

        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldReturnNull()
    {
        var invalidToken = "invalid.jwt.token";

        var principal = _tokenService.GetPrincipalFromExpiredToken(invalidToken);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WithWrongSignature_ShouldReturnNull()
    {
        var differentSettings = new JwtSettings
        {
            SecretKey = "DifferentSecretKeyThatWillProduceInvalidSignature123456789012345678",
            Issuer = _jwtSettings.Value.Issuer,
            Audience = _jwtSettings.Value.Audience,
            ExpiryMinutes = 60
        };
        var differentTokenService = new TokenService(
            Options.Create(differentSettings),
            _context,
            _loggerMock.Object);

        var user = TestDataBuilder.CreateTestUser();
        user.Id = 1;
        var token = await differentTokenService.GenerateAccessTokenAsync(user, new List<string>());

        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ShouldReturnTrue()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.Id = 1;
        var token = await _tokenService.GenerateAccessTokenAsync(user, new List<string> { "user" });

        var result = _tokenService.ValidateToken(token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_WithExpiredToken_ShouldReturnFalse()
    {
        var expiredSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.Value.SecretKey,
            Issuer = _jwtSettings.Value.Issuer,
            Audience = _jwtSettings.Value.Audience,
            ExpiryMinutes = -5
        };
        var expiredTokenService = new TokenService(Options.Create(expiredSettings), _context, _loggerMock.Object);

        var user = TestDataBuilder.CreateTestUser();
        user.Id = 1;
        var expiredToken = await expiredTokenService.GenerateAccessTokenAsync(user, new List<string>());

        var result = _tokenService.ValidateToken(expiredToken);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse()
    {
        var result = _tokenService.ValidateToken("invalid.token.here");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ShouldReturnFalse()
    {
        var result = _tokenService.ValidateToken("not-a-jwt-at-all");

        result.Should().BeFalse();
    }

    private static bool IsBase64String(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
