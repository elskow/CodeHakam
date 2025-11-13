using AccountService.Constants;
using AccountService.Data;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Events;
using AccountService.Models;
using AccountService.Services.Interfaces;
using Casbin;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services.Implementations;

public sealed class AdminService(
    ApplicationDbContext context,
    ICasbinPolicyService policyService,
    IEventPublisher eventPublisher,
    ILogger<AdminService> logger,
    IEnforcer enforcer)
    : IAdminService
{
    public async Task<PaginatedResponse<UserListItemDto>> GetUsersAsync(UserSearchRequest request)
    {
        var query = context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.UserName!.ToLower().Contains(searchTerm) ||
                u.FullName != null && u.FullName.ToLower().Contains(searchTerm) ||
                u.Email!.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            query = query.Where(u => u.Country == request.Country);
        }

        if (request.MinRating.HasValue)
        {
            query = query.Where(u => u.Rating >= request.MinRating.Value);
        }

        if (request.MaxRating.HasValue)
        {
            query = query.Where(u => u.Rating <= request.MaxRating.Value);
        }

        if (request.IsVerified.HasValue)
        {
            query = query.Where(u => u.IsVerified == request.IsVerified.Value);
        }

        if (request.IsBanned.HasValue)
        {
            query = query.Where(u => u.IsBanned == request.IsBanned.Value);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "username" => request.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(u => u.UserName)
                : query.OrderByDescending(u => u.UserName),
            "rating" => request.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(u => u.Rating)
                : query.OrderByDescending(u => u.Rating),
            "createdat" => request.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.Rating)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var userDtos = items.Select(u => new UserListItemDto
        {
            Id = u.Id,
            Username = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            FullName = u.FullName,
            Rating = u.Rating,
            IsVerified = u.IsVerified,
            IsBanned = u.IsBanned,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToList();

        return new PaginatedResponse<UserListItemDto>
        {
            Items = userDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.Page > 1,
            HasNextPage = request.Page < totalPages
        };
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(long userId)
    {
        var user = await context.Users
            .Include(u => u.Statistics)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Country = user.Country,
            Organization = user.Organization,
            Rating = user.Rating,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            Statistics = user.Statistics != null ? MapToUserStatisticsDto(user.Statistics) : null
        };
    }

    public async Task<bool> BanUserAsync(long userId, string reason, long adminUserId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Attempted to ban non-existent user {UserId}", userId);
            return false;
        }

        if (user.IsBanned)
        {
            logger.LogWarning("User {UserId} is already banned", userId);
            return false;
        }

        user.IsBanned = true;
        user.BanReason = reason;
        user.BannedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} banned by admin {AdminUserId}. Reason: {Reason}",
            userId, adminUserId, reason);

        // Publish event
        await eventPublisher.PublishUserBannedAsync(new UserBannedEvent
        {
            UserId = userId,
            BannedBy = adminUserId,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        });

        return true;
    }

    public async Task<bool> UnbanUserAsync(long userId, long adminUserId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Attempted to unban non-existent user {UserId}", userId);
            return false;
        }

        if (!user.IsBanned)
        {
            logger.LogWarning("User {UserId} is not banned", userId);
            return false;
        }

        user.IsBanned = false;
        user.BanReason = null;
        user.BannedAt = null;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} unbanned by admin {AdminUserId}", userId, adminUserId);

        // Publish event (Note: No specific UnbanEvent in IEventPublisher, using RoleAssigned as workaround or skip)
        // await _eventPublisher.PublishUserUnbannedAsync(...);
        logger.LogInformation("User {UserId} unbanned - event publishing skipped (no unban event defined)", userId);

        return true;
    }

    public async Task<bool> AssignRoleAsync(long userId, long roleId, long adminUserId)
    {
        var user = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            logger.LogWarning("Attempted to assign role to non-existent user {UserId}", userId);
            return false;
        }

        var role = await context.Roles.FindAsync(roleId);
        if (role == null)
        {
            logger.LogWarning("Attempted to assign non-existent role {RoleId}", roleId);
            return false;
        }

        // Check if user already has this role
        if (user.UserRoles.Any(ur => ur.RoleId == roleId))
        {
            logger.LogWarning("User {UserId} already has role {RoleId}", userId, roleId);
            return false;
        }

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = adminUserId,
            AssignedAt = DateTime.UtcNow
        };

        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        logger.LogInformation("Role {RoleName} (ID: {RoleId}) assigned to user {UserId} by admin {AdminUserId}",
            role.Name, roleId, userId, adminUserId);

        // Reload policies to reflect the new role assignment
        await ReloadPoliciesAsync();

        // Publish event
        await eventPublisher.PublishRoleAssignedAsync(new RoleAssignedEvent
        {
            UserId = userId,
            RoleName = role.Name,
            AssignedBy = adminUserId,
            Timestamp = DateTime.UtcNow
        });

        return true;
    }

    public async Task<bool> RemoveRoleAsync(long userId, long roleId, long adminUserId)
    {
        var userRole = await context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
        {
            logger.LogWarning("User {UserId} does not have role {RoleId}", userId, roleId);
            return false;
        }

        // Prevent removing the last role (user should always have at least "user" role)
        var userRoleCount = await context.UserRoles.CountAsync(ur => ur.UserId == userId);
        if (userRoleCount <= 1)
        {
            logger.LogWarning("Cannot remove the last role from user {UserId}", userId);
            return false;
        }

        var roleName = userRole.Role.Name;
        context.UserRoles.Remove(userRole);
        await context.SaveChangesAsync();

        logger.LogInformation("Role {RoleName} (ID: {RoleId}) removed from user {UserId} by admin {AdminUserId}",
            roleName, roleId, userId, adminUserId);

        // Reload policies to reflect the role removal
        await ReloadPoliciesAsync();

        // Publish event (Note: No specific RoleRemovedEvent in IEventPublisher, skip or use existing event)
        // await _eventPublisher.PublishRoleRemovedAsync(...);
        logger.LogInformation(
            "Role removed from user {UserId} - event publishing skipped (no role removed event defined)", userId);

        return true;
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await context.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetAllPermissionsAsync()
    {
        return await context.Permissions
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync();
    }

    public async Task<List<Role>> GetUserRolesAsync(long userId)
    {
        return await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(long roleId)
    {
        return await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync();
    }

    public async Task ReloadPoliciesAsync()
    {
        await policyService.LoadPoliciesIntoEnforcerAsync(enforcer);
        logger.LogInformation("Casbin policies reloaded manually");
    }

    public async Task<bool> VerifyUserEmailAsync(long userId, long adminUserId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Attempted to verify email for non-existent user {UserId}", userId);
            return false;
        }

        if (user.IsVerified)
        {
            logger.LogWarning("User {UserId} is already verified", userId);
            return false;
        }

        user.IsVerified = true;
        user.EmailConfirmed = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiry = null;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} email verified by admin {AdminUserId}", userId, adminUserId);

        // Publish event (Note: No specific EmailVerifiedEvent in IEventPublisher, skip)
        // await _eventPublisher.PublishEmailVerifiedAsync(...);
        logger.LogInformation(
            "User email verified {UserId} - event publishing skipped (no email verified event defined)", userId);

        return true;
    }

    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        var totalUsers = await context.Users.CountAsync();
        var activeUsers = await context.Users.CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-ApplicationConstants.Thresholds.ActiveUserDays));
        var bannedUsers = await context.Users.CountAsync(u => u.IsBanned);
        var verifiedUsers = await context.Users.CountAsync(u => u.IsVerified);
        var lastUserRegistration = await context.Users.OrderByDescending(u => u.CreatedAt).Select(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        return new SystemStatisticsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            BannedUsers = bannedUsers,
            VerifiedUsers = verifiedUsers,
            TotalProblems = 0, // To be populated by Content Service
            TotalSubmissions = 0, // To be populated by Execution Service
            TotalContests = 0, // To be populated by Contest Service
            LastUserRegistration = lastUserRegistration == default ? null : lastUserRegistration
        };
    }

    private UserStatisticsDto MapToUserStatisticsDto(UserStatistics stats)
    {
        return new UserStatisticsDto
        {
            ProblemsSolved = stats.ProblemsSolved,
            ContestsParticipated = stats.ContestsParticipated,
            TotalSubmissions = stats.TotalSubmissions,
            AcceptedSubmissions = stats.AcceptedSubmissions,
            AcceptanceRate = stats.TotalSubmissions > 0
                ? Math.Round((decimal)stats.AcceptedSubmissions / stats.TotalSubmissions * 100, decimals: 2)
                : 0,
            MaxStreak = stats.MaxStreak,
            CurrentStreak = stats.CurrentStreak,
            LastSubmissionDate = stats.LastSubmissionDate,
            EasySolved = stats.EasySolved,
            MediumSolved = stats.MediumSolved,
            HardSolved = stats.HardSolved,
            GlobalRank = stats.GlobalRank,
            CountryRank = stats.CountryRank
        };
    }
}
