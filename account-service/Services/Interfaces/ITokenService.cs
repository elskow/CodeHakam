using AccountService.Models;

namespace AccountService.Services.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user, IEnumerable<string> roles);
    Task<string> GenerateRefreshTokenAsync();
    Task<string> GenerateEmailVerificationTokenAsync();
    Task<string> GeneratePasswordResetTokenAsync();
    string HashToken(string token);
    Task<bool> ValidateRefreshTokenAsync(string token, long userId);
    Task RevokeRefreshTokenAsync(string token, string? revokedByIp = null);
    Task RevokeAllUserRefreshTokensAsync(long userId);
}
