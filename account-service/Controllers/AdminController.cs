using System.Security.Claims;
using AccountService.Authorization;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Extensions;
using AccountService.Models;
using AccountService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController(IAdminService adminService, ILogger<AdminController> logger)
    : ControllerBase
{
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
    ///     Get all users with filtering and pagination (admin only)
    /// </summary>
    [HttpGet("users")]
    [RequirePermission("user_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request)
    {
        // Normalize empty strings to null
        request = request.Normalize();

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid search parameters", ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []
            )));
        }

        var result = await adminService.GetUsersAsync(request);
        return Ok(ApiResponse<PaginatedResponse<UserListItemDto>>.SuccessResponse(result));
    }

    /// <summary>
    ///     Get user by ID (admin view with full details)
    /// </summary>
    [HttpGet("users/{userId:long}")]
    [RequirePermission("user_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(long userId)
    {
        var user = await adminService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
        }

        return Ok(ApiResponse<UserProfileDto>.SuccessResponse(user));
    }

    /// <summary>
    ///     Ban user
    /// </summary>
    [HttpPost("users/{userId:long}/ban")]
    [RequirePermission("user_ban", "manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BanUser(long userId, [FromBody] BanUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []
            )));
        }

        var adminUserId = GetCurrentUserId();
        var result = await adminService.BanUserAsync(userId, request.Reason, adminUserId);

        if (!result)
        {
            return BadRequest(
                ApiResponse<object>.ErrorResponse("Failed to ban user. User may not exist or is already banned."));
        }

        logger.LogInformation("User {UserId} banned by admin {AdminUserId}", userId, adminUserId);
        return Ok(ApiResponse<object>.SuccessResponse(new { userId }, "User banned successfully"));
    }

    /// <summary>
    ///     Unban user
    /// </summary>
    [HttpPost("users/{userId:long}/unban")]
    [RequirePermission("user_ban", "manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnbanUser(long userId)
    {
        var adminUserId = GetCurrentUserId();
        var result = await adminService.UnbanUserAsync(userId, adminUserId);

        if (!result)
        {
            return BadRequest(
                ApiResponse<object>.ErrorResponse("Failed to unban user. User may not exist or is not banned."));
        }

        logger.LogInformation("User {UserId} unbanned by admin {AdminUserId}", userId, adminUserId);
        return Ok(ApiResponse<object>.SuccessResponse(new { userId }, "User unbanned successfully"));
    }

    /// <summary>
    ///     Assign role to user
    /// </summary>
    [HttpPost("users/{userId:long}/roles")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(long userId, [FromBody] AssignRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []
            )));
        }

        var adminUserId = GetCurrentUserId();
        var result = await adminService.AssignRoleAsync(userId, request.RoleId, adminUserId);

        if (!result)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Failed to assign role. User or role may not exist, or user already has this role."));
        }

        logger.LogInformation("Role {RoleId} assigned to user {UserId} by admin {AdminUserId}",
            request.RoleId, userId, adminUserId);
        return Ok(ApiResponse<object>.SuccessResponse(new { userId, roleId = request.RoleId },
            "Role assigned successfully"));
    }

    /// <summary>
    ///     Remove role from user
    /// </summary>
    [HttpDelete("users/{userId:long}/roles/{roleId:long}")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveRole(long userId, long roleId)
    {
        var adminUserId = GetCurrentUserId();
        var result = await adminService.RemoveRoleAsync(userId, roleId, adminUserId);

        if (!result)
        {
            return BadRequest(
                ApiResponse<object>.ErrorResponse(
                    "Failed to remove role. User may not have this role or it's the last role."));
        }

        logger.LogInformation("Role {RoleId} removed from user {UserId} by admin {AdminUserId}",
            roleId, userId, adminUserId);
        return Ok(ApiResponse<object>.SuccessResponse(new { userId, roleId }, "Role removed successfully"));
    }

    /// <summary>
    ///     Get all available roles
    /// </summary>
    [HttpGet("roles")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<List<Role>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await adminService.GetAllRolesAsync();
        return Ok(ApiResponse<List<Role>>.SuccessResponse(roles));
    }

    /// <summary>
    ///     Get all permissions
    /// </summary>
    [HttpGet("permissions")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<List<Permission>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await adminService.GetAllPermissionsAsync();
        return Ok(ApiResponse<List<Permission>>.SuccessResponse(permissions));
    }

    /// <summary>
    ///     Get user's roles
    /// </summary>
    [HttpGet("users/{userId:long}/roles")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<List<Role>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(long userId)
    {
        var roles = await adminService.GetUserRolesAsync(userId);
        return Ok(ApiResponse<List<Role>>.SuccessResponse(roles));
    }

    /// <summary>
    ///     Get role's permissions
    /// </summary>
    [HttpGet("roles/{roleId:long}/permissions")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<List<Permission>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(long roleId)
    {
        var permissions = await adminService.GetRolePermissionsAsync(roleId);
        return Ok(ApiResponse<List<Permission>>.SuccessResponse(permissions));
    }

    /// <summary>
    ///     Reload Casbin policies manually
    /// </summary>
    [HttpPost("casbin/reload")]
    [RequirePermission("role_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReloadPolicies()
    {
        var adminUserId = GetCurrentUserId();
        await adminService.ReloadPoliciesAsync();

        logger.LogInformation("Casbin policies reloaded by admin {AdminUserId}", adminUserId);
        return Ok(ApiResponse<object>.SuccessResponse(new { reloadedAt = DateTime.UtcNow },
            "Policies reloaded successfully"));
    }

    /// <summary>
    ///     Verify user email (admin override)
    /// </summary>
    [HttpPost("users/{userId:long}/verify-email")]
    [RequirePermission("user_management", "manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyUserEmail(long userId)
    {
        var adminUserId = GetCurrentUserId();
        var result = await adminService.VerifyUserEmailAsync(userId, adminUserId);

        if (!result)
        {
            return BadRequest(
                ApiResponse<object>.ErrorResponse(
                    "Failed to verify user email. User may not exist or is already verified."));
        }

        logger.LogInformation("User {UserId} email verified by admin {AdminUserId}", userId, adminUserId);
        return Ok(ApiResponse<object>.SuccessResponse(new { userId }, "User email verified successfully"));
    }

    /// <summary>
    ///     Get system statistics
    /// </summary>
    [HttpGet("statistics")]
    [RequirePermission("analytics", "read")]
    [ProducesResponseType(typeof(ApiResponse<SystemStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemStatistics()
    {
        var statistics = await adminService.GetSystemStatisticsAsync();
        return Ok(ApiResponse<SystemStatisticsDto>.SuccessResponse(statistics));
    }
}
