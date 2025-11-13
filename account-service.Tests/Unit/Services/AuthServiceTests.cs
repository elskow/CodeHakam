using AccountService.Data;
using AccountService.DTOs;
using AccountService.Models;
using AccountService.Services.Interfaces;
using AccountService.Services.Implementations;
using AccountService.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.Unit.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;

    public AuthServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"AuthServiceTests_{Guid.NewGuid()}");

        var userStore = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        var signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _context,
            _userManagerMock.Object,
            signInManagerMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task LogoutAsync_WithValidToken_ShouldRevokeTokenAndReturnSuccess()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.UserRoles = new List<UserRole>(); // Initialize navigation property

        var rawToken = "raw_refresh_token";
        var tokenHash = "valid_token_hash_12345";
        var refreshToken = TestDataBuilder.CreateRefreshToken(
            user.Id,
            tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7));

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync(); // Save user first to get ID

        refreshToken.UserId = user.Id; // Ensure FK is set
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(tokenHash);

        var (success, error) = await _authService.LogoutAsync(user.Id, rawToken, "127.0.0.1");

        success.Should().BeTrue();
        error.Should().BeNull();

        var revokedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        revokedToken.Should().NotBeNull();
        revokedToken!.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedByIp.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "non_existent_token";
        var hashedToken = "hashed_non_existent";

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(hashedToken);

        var (success, error) = await _authService.LogoutAsync(user.Id, rawToken, "127.0.0.1");

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LogoutAsync_WithAlreadyRevokedToken_ShouldReturnUnauthorized()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.UserRoles = new List<UserRole>(); // Initialize navigation property

        var rawToken = "revoked_token";
        var tokenHash = "revoked_token_hash";

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync(); // Save user first to get ID

        var refreshToken = TestDataBuilder.CreateRefreshToken(
            user.Id,
            tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            revokedAt: DateTime.UtcNow.AddHours(-1));

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(tokenHash);

        var (success, error) = await _authService.LogoutAsync(user.Id, rawToken, "127.0.0.1");

        success.Should().BeFalse();
        error.Should().Contain("revoked");
    }

    [Fact]
    public async Task LogoutAsync_WithExpiredToken_ShouldReturnUnauthorized()
    {
        var user = TestDataBuilder.CreateTestUser();
        var rawToken = "expired_token";
        var tokenHash = "expired_token_hash";
        var refreshToken = TestDataBuilder.CreateRefreshToken(
            user.Id,
            tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(-1));

        await _context.Users.AddAsync(user);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(tokenHash);

        var (success, error) = await _authService.LogoutAsync(user.Id, rawToken, "127.0.0.1");

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LogoutAsync_WithTokenBelongingToDifferentUser_ShouldReturnUnauthorized()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "user1", email: "user1@example.com");
        var user2 = TestDataBuilder.CreateTestUser(username: "user2", email: "user2@example.com");
        user2.Id = 2;

        var rawToken = "user2_token";
        var tokenHash = "user2_token_hash";
        var refreshToken = TestDataBuilder.CreateRefreshToken(
            user2.Id,
            tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7));

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(tokenHash);

        var (success, error) = await _authService.LogoutAsync(user1.Id, rawToken, "127.0.0.1");

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.UserRoles = new List<UserRole>(); // Initialize navigation property

        var oldRawToken = "old_refresh_token";
        var oldTokenHash = "old_token_hash";

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync(); // Save user first to get ID

        var oldRefreshToken = TestDataBuilder.CreateRefreshToken(
            user.Id,
            oldTokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7));
        oldRefreshToken.User = user; // Set navigation property

        await _context.RefreshTokens.AddAsync(oldRefreshToken);
        await _context.SaveChangesAsync();

        var newAccessToken = "new_access_token";
        var newRefreshTokenValue = "new_refresh_token";
        var newRefreshTokenHash = "new_refresh_token_hash";

        _tokenServiceMock
            .Setup(x => x.HashToken(oldRawToken))
            .Returns(oldTokenHash);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(newAccessToken);

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync())
            .ReturnsAsync(newRefreshTokenValue);

        _tokenServiceMock
            .Setup(x => x.HashToken(newRefreshTokenValue))
            .Returns(newRefreshTokenHash);

        var request = new RefreshTokenRequest { RefreshToken = oldRawToken };

        var (success, response, error) = await _authService.RefreshTokenAsync(request, "127.0.0.1");

        success.Should().BeTrue();
        error.Should().BeNull();
        response.Should().NotBeNull();
        response!.AccessToken.Should().Be(newAccessToken);
        response.RefreshToken.Should().Be(newRefreshTokenValue);
        response.ExpiresIn.Should().BeGreaterThan(0);

        var oldToken = await _context.RefreshTokens.FindAsync(oldRefreshToken.Id);
        oldToken.Should().NotBeNull();
        oldToken!.RevokedAt.Should().NotBeNull();
        oldToken.ReplacedByToken.Should().Be(newRefreshTokenHash);

        var newToken = _context.RefreshTokens
            .FirstOrDefault(t => t.TokenHash == newRefreshTokenHash);
        newToken.Should().NotBeNull();
        newToken!.UserId.Should().Be(user.Id);
        newToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var rawToken = "non_existent_token";
        var hashedToken = "hashed_non_existent";

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(hashedToken);

        var request = new RefreshTokenRequest { RefreshToken = rawToken };

        var (success, response, error) = await _authService.RefreshTokenAsync(request, "127.0.0.1");

        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldReturnUnauthorized()
    {
        var user = TestDataBuilder.CreateTestUser();
        var rawToken = "revoked_token";
        var tokenHash = "revoked_token_hash";
        var refreshToken = TestDataBuilder.CreateRefreshToken(
            user.Id,
            tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7),
            revokedAt: DateTime.UtcNow.AddHours(-1));

        await _context.Users.AddAsync(user);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(tokenHash);

        var request = new RefreshTokenRequest { RefreshToken = rawToken };

        var (success, response, error) = await _authService.RefreshTokenAsync(request, "127.0.0.1");

        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnUnauthorized()
    {
        var user = TestDataBuilder.CreateTestUser();
        var rawToken = "expired_token";
        var tokenHash = "expired_token_hash";
        var refreshToken = TestDataBuilder.CreateRefreshToken(
            user.Id,
            tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(-1));

        await _context.Users.AddAsync(user);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _tokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns(tokenHash);

        var request = new RefreshTokenRequest { RefreshToken = rawToken };

        var (success, response, error) = await _authService.RefreshTokenAsync(request, "127.0.0.1");

        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_ShouldChangePassword()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, "OldPassword123!"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var (success, error) = await _authService.ChangePasswordAsync(user.Id, request);

        success.Should().BeTrue();
        error.Should().BeNull();

        _userManagerMock.Verify(
            x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword123!"),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ShouldFail()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        var identityError = new IdentityError { Description = "Incorrect password" };
        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "WrongPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var (success, error) = await _authService.ChangePasswordAsync(user.Id, request);

        success.Should().BeFalse();
        error.Should().Contain("password");

        _userManagerMock.Verify(
            x => x.ChangePasswordAsync(user, "WrongPassword123!", "NewPassword123!"),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ShouldFail()
    {
        _userManagerMock
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var (success, error) = await _authService.ChangePasswordAsync(userId: 999, request);

        success.Should().BeFalse();
        error.Should().Be("User not found");
    }
}
