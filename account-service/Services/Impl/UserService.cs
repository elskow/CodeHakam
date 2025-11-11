using AccountService.Data;
using AccountService.DTOs;
using AccountService.Enums;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services.Impl;

public sealed class UserService(ApplicationDbContext context, ILogger<UserService> logger) : IUserService
{
    public async Task<UserProfileDto?> GetUserProfileAsync(long userId)
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

        return MapToUserProfileDto(user);
    }

    public async Task<UserProfileDto?> GetCurrentUserProfileAsync(long currentUserId)
    {
        return await GetUserProfileAsync(currentUserId);
    }

    public async Task<UserProfileDto?> GetPublicUserProfileAsync(long userId)
    {
        var user = await context.Users
            .Include(u => u.Statistics)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.IsBanned)
        {
            return null;
        }

        return MapToUserProfileDto(user, true);
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(long userId, UpdateProfileRequest request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        if (request.FullName != null)
        {
            user.FullName = request.FullName;
        }

        if (request.Bio != null)
        {
            user.Bio = request.Bio;
        }

        if (request.Country != null)
        {
            user.Country = request.Country;
        }

        if (request.Organization != null)
        {
            user.Organization = request.Organization;
        }

        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User profile updated for user {UserId}", userId);

        return await GetUserProfileAsync(userId) ??
               throw new InvalidOperationException("Failed to retrieve updated profile");
    }

    public async Task<UserStatisticsDto?> GetUserStatisticsAsync(long userId)
    {
        var stats = await context.UserStatistics
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (stats == null)
        {
            return null;
        }

        return MapToUserStatisticsDto(stats);
    }

    public async Task<List<RatingHistoryDto>> GetRatingHistoryAsync(long userId, int limit = 50)
    {
        var history = await context.RatingHistory
            .Where(rh => rh.UserId == userId)
            .OrderByDescending(rh => rh.ChangedAt)
            .Take(limit)
            .ToListAsync();

        return history.Select(rh => new RatingHistoryDto
        {
            Id = rh.Id,
            ContestId = rh.ContestId,
            ContestName = null, // To be populated by Contest Service
            OldRating = rh.OldRating,
            NewRating = rh.NewRating,
            RatingChange = rh.NewRating - rh.OldRating,
            Rank = rh.Rank,
            ChangedAt = rh.ChangedAt
        }).ToList();
    }

    public async Task<List<AchievementDto>> GetUserAchievementsAsync(long userId)
    {
        var achievements = await context.Achievements
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.EarnedAt)
            .ToListAsync();

        return achievements.Select(a => new AchievementDto
        {
            Id = a.Id,
            AchievementType = a.AchievementType,
            Title = a.Title,
            Description = a.Description,
            IconUrl = a.IconUrl,
            Points = a.Points,
            EarnedAt = a.EarnedAt
        }).ToList();
    }

    public async Task<UserSettingsDto?> GetUserSettingsAsync(long userId)
    {
        var settings = await context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            return null;
        }

        return new UserSettingsDto
        {
            LanguagePreference = settings.LanguagePreference,
            Theme = settings.Theme,
            EmailNotifications = settings.EmailNotifications,
            ContestReminders = settings.ContestReminders,
            SolutionVisibility = settings.SolutionVisibility,
            ShowRating = settings.ShowRating,
            Timezone = settings.Timezone
        };
    }

    public async Task<UserSettingsDto> UpdateUserSettingsAsync(long userId, UpdateUserSettingsRequest request)
    {
        var settings = await context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings if they don't exist
            settings = new UserSettings
            {
                UserId = userId
            };
            context.UserSettings.Add(settings);
        }

        if (request.LanguagePreference != null)
        {
            // Normalize to lowercase for consistency
            settings.LanguagePreference = request.LanguagePreference.ToLower();
        }

        if (request.Theme != null)
        {
            // Normalize to lowercase for consistency
            settings.Theme = request.Theme.ToLower();
        }

        if (request.EmailNotifications.HasValue)
        {
            settings.EmailNotifications = request.EmailNotifications.Value;
        }

        if (request.ContestReminders.HasValue)
        {
            settings.ContestReminders = request.ContestReminders.Value;
        }

        if (request.SolutionVisibility != null)
        {
            // Normalize to lowercase for consistency
            settings.SolutionVisibility = request.SolutionVisibility.ToLower();
        }

        if (request.ShowRating.HasValue)
        {
            settings.ShowRating = request.ShowRating.Value;
        }

        if (request.Timezone != null)
        {
            settings.Timezone = request.Timezone;
        }

        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User settings updated for user {UserId}", userId);

        return await GetUserSettingsAsync(userId) ??
               throw new InvalidOperationException("Failed to retrieve updated settings");
    }

    public async Task<PaginatedResponse<UserListItemDto>> SearchUsersAsync(UserSearchRequest request)
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
                (u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) ||
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

    public async Task<bool> UserExistsAsync(long userId)
    {
        return await context.Users.AnyAsync(u => u.Id == userId);
    }

    private UserProfileDto MapToUserProfileDto(User user, bool isPublic = false)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = isPublic ? string.Empty : user.Email ?? string.Empty,
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

    private UserStatisticsDto MapToUserStatisticsDto(UserStatistics stats)
    {
        return new UserStatisticsDto
        {
            ProblemsSolved = stats.ProblemsSolved,
            ContestsParticipated = stats.ContestsParticipated,
            TotalSubmissions = stats.TotalSubmissions,
            AcceptedSubmissions = stats.AcceptedSubmissions,
            AcceptanceRate = stats.TotalSubmissions > 0
                ? Math.Round((decimal)stats.AcceptedSubmissions / stats.TotalSubmissions * 100, 2)
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
