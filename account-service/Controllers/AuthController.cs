using System.Security.Claims;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IUserService userService, ILogger<AuthController> logger)
    : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IUserService _userService = userService;

    /// <summary>
    ///     Register a new user account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Validation failed",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, response, error) = await authService.RegisterAsync(request, ipAddress);

        if (!success)
        {
            var statusCode = error?.Contains("already") == true
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, ApiResponse<object>.ErrorResponse(error!));
        }

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<RegisterResponse>.SuccessResponse(response!)
        );
    }

    /// <summary>
    ///     Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Validation failed",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, response, error) = await authService.LoginAsync(request, ipAddress);

        if (!success)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(response!, "Login successful"));
    }

    /// <summary>
    ///     Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid refresh token"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, response, error) = await authService.RefreshTokenAsync(request, ipAddress);

        if (!success)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<RefreshTokenResponse>.SuccessResponse(response!));
    }

    /// <summary>
    ///     Logout and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, error) = await authService.LogoutAsync(userId, request.RefreshToken, ipAddress);

        if (!success)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully", "Logged out successfully"));
    }

    /// <summary>
    ///     Verify email with token
    /// </summary>
    [HttpGet("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Token is required"));
        }

        var (success, error) = await authService.VerifyEmailAsync(token);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<string>.SuccessResponse("Email verified successfully", "Email verified successfully"));
    }

    /// <summary>
    ///     Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid email address"));
        }

        var (success, error) = await authService.ForgotPasswordAsync(request.Email);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<string>.SuccessResponse(
            "If an account with that email exists, a password reset link has been sent",
            "If an account with that email exists, a password reset link has been sent"
        ));
    }

    /// <summary>
    ///     Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Validation failed",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        var (success, error) = await authService.ResetPasswordAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<string>.SuccessResponse("Password reset successfully", "Password reset successfully"));
    }

    /// <summary>
    ///     Change password (requires authentication)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Validation failed",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user"));
        }

        var (success, error) = await authService.ChangePasswordAsync(userId, request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(error!));
        }

        return Ok(ApiResponse<string>.SuccessResponse("Password changed successfully",
            "Password changed successfully"));
    }

    /// <summary>
    ///     Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "auth",
            timestamp = DateTime.UtcNow
        });
    }
}
