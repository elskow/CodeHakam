using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AccountService.Configuration;
using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using AccountService.Services.Interfaces;
namespace AccountService.Services.Implementations;

public sealed class TokenService(
    IOptions<JwtSettings> jwtSettings,
    ApplicationDbContext context,
    ILogger<TokenService> logger)
    : ITokenService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public Task<string> GenerateAccessTokenAsync(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("username", user.UserName ?? string.Empty),
            new("rating", user.Rating.ToString()),
            new("is_verified", user.IsVerified.ToString()),
            new("is_banned", user.IsBanned.ToString())
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        return Task.FromResult(token);
    }

    public Task<string> GenerateEmailVerificationTokenAsync()
    {
        return Task.FromResult(GenerateSecureToken());
    }

    public Task<string> GeneratePasswordResetTokenAsync()
    {
        return Task.FromResult(GenerateSecureToken());
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, long userId)
    {
        try
        {
            var tokenHash = HashToken(token);
            var refreshToken = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.UserId == userId);

            if (refreshToken == null)
            {
                return false;
            }

            // Check if token is active (not revoked and not expired)
            return refreshToken.IsActive;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task RevokeRefreshTokenAsync(string token, string? revokedByIp = null)
    {
        try
        {
            var tokenHash = HashToken(token);
            var refreshToken = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (refreshToken is { IsActive: true })
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = revokedByIp;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking refresh token");
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(long userId)
    {
        try
        {
            var refreshTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Revoked all refresh tokens for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking all refresh tokens for user {UserId}", userId);
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false, // Don't validate expiry
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate expired token");
            return null;
        }
    }

    public bool ValidateToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateSecureToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
