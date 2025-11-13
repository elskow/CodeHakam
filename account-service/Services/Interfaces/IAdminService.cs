using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Models;

namespace AccountService.Services.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Get all users with pagination and filtering
    /// </summary>
    Task<PaginatedResponse<UserListItemDto>> GetUsersAsync(UserSearchRequest request);

    /// <summary>
    /// Get user by ID (admin view with full details)
    /// </summary>
    Task<UserProfileDto?> GetUserByIdAsync(long userId);

    /// <summary>
    /// Ban user with reason
    /// </summary>
    Task<bool> BanUserAsync(long userId, string reason, long adminUserId);

    /// <summary>
    /// Unban user
    /// </summary>
    Task<bool> UnbanUserAsync(long userId, long adminUserId);

    /// <summary>
    /// Assign role to user
    /// </summary>
    Task<bool> AssignRoleAsync(long userId, long roleId, long adminUserId);

    /// <summary>
    /// Remove role from user
    /// </summary>
    Task<bool> RemoveRoleAsync(long userId, long roleId, long adminUserId);

    /// <summary>
    /// Get all roles
    /// </summary>
    Task<List<Role>> GetAllRolesAsync();

    /// <summary>
    /// Get all permissions
    /// </summary>
    Task<List<Permission>> GetAllPermissionsAsync();

    /// <summary>
    /// Get user's roles
    /// </summary>
    Task<List<Role>> GetUserRolesAsync(long userId);

    /// <summary>
    /// Get role's permissions
    /// </summary>
    Task<List<Permission>> GetRolePermissionsAsync(long roleId);

    /// <summary>
    /// Reload Casbin policies (trigger policy sync)
    /// </summary>
    Task ReloadPoliciesAsync();

    /// <summary>
    /// Verify user email (admin override)
    /// </summary>
    Task<bool> VerifyUserEmailAsync(long userId, long adminUserId);

    /// <summary>
    /// Get system statistics
    /// </summary>
    Task<SystemStatisticsDto> GetSystemStatisticsAsync();
}
