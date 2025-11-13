using AccountService.Authorization;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Extensions;
using AccountService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccountService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Get current authenticated user's full profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var profile = await _userService.GetUserProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User profile not found"));
        }

        return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile));
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            )));
        }

        try
        {
            var userId = GetCurrentUserId();
            var updatedProfile = await _userService.UpdateUserProfileAsync(userId, request);
            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(updatedProfile, "Profile updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update user profile");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get current user's settings
    /// </summary>
    [HttpGet("me/settings")]
    [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _userService.GetUserSettingsAsync(userId);

        if (settings == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User settings not found"));
        }

        return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(settings));
    }

    /// <summary>
    /// Update current user's settings
    /// </summary>
    [HttpPut("me/settings")]
    [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMySettings([FromBody] UpdateUserSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            )));
        }

        try
        {
            var userId = GetCurrentUserId();
            var updatedSettings = await _userService.UpdateUserSettingsAsync(userId, request);
            return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(updatedSettings, "Settings updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update user settings");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get user profile by ID (public view)
    /// </summary>
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(long id)
    {
        var profile = await _userService.GetPublicUserProfileAsync(id);

        if (profile == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
        }

        return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile));
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("{id:long}/statistics")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserStatistics(long id)
    {
        var statistics = await _userService.GetUserStatisticsAsync(id);

        if (statistics == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User statistics not found"));
        }

        return Ok(ApiResponse<UserStatisticsDto>.SuccessResponse(statistics));
    }

    /// <summary>
    /// Get user rating history
    /// </summary>
    [HttpGet("{id:long}/rating-history")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<RatingHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRatingHistory(long id, [FromQuery] int limit = 50)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Limit must be between 1 and 100"));
        }

        var history = await _userService.GetRatingHistoryAsync(id, limit);
        return Ok(ApiResponse<List<RatingHistoryDto>>.SuccessResponse(history));
    }

    /// <summary>
    /// Get user achievements
    /// </summary>
    [HttpGet("{id:long}/achievements")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<AchievementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAchievements(long id)
    {
        var achievements = await _userService.GetUserAchievementsAsync(id);
        return Ok(ApiResponse<List<AchievementDto>>.SuccessResponse(achievements));
    }

    /// <summary>
    /// Search users with filters and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchUsers([FromQuery] UserSearchRequest request)
    {
        // Normalize empty strings to null
        request = request.Normalize();

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid search parameters", ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            )));
        }

        var result = await _userService.SearchUsersAsync(request);
        return Ok(ApiResponse<PaginatedResponse<UserListItemDto>>.SuccessResponse(result));
    }
}
