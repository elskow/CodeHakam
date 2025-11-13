using AccountService.DTOs;
using AccountService.DTOs.Common;
namespace AccountService.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, RegisterResponse? Response, string? Error)> RegisterAsync(RegisterRequest request, string? ipAddress = null);
    Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null);
    Task<(bool Success, string? Error)> LogoutAsync(long userId, string refreshToken, string? ipAddress = null);
    Task<(bool Success, string? Error)> VerifyEmailAsync(string token);
    Task<(bool Success, string? Error)> ForgotPasswordAsync(string email);
    Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request);
    Task<(bool Success, string? Error)> ChangePasswordAsync(long userId, ChangePasswordRequest request);
}