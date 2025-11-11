using System.ComponentModel.DataAnnotations;
using AccountService.Validation;

namespace AccountService.DTOs;

// User Profile
public record UserProfileDto
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? Country { get; init; }
    public string? Organization { get; init; }
    public int Rating { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public List<string> Roles { get; init; } = new();
    public UserStatisticsDto? Statistics { get; init; }
}

// Update Profile
public record UpdateProfileRequest
{
    [StringLength(100)]
    public string? FullName { get; init; }

    [StringLength(1000)]
    public string? Bio { get; init; }

    [StringLength(2)]
    public string? Country { get; init; }

    [StringLength(200)]
    public string? Organization { get; init; }

    [Url]
    [StringLength(500)]
    public string? AvatarUrl { get; init; }
}

// User Statistics
public record UserStatisticsDto
{
    public int ProblemsSolved { get; init; }
    public int ContestsParticipated { get; init; }
    public int TotalSubmissions { get; init; }
    public int AcceptedSubmissions { get; init; }
    public decimal AcceptanceRate { get; init; }
    public int MaxStreak { get; init; }
    public int CurrentStreak { get; init; }
    public DateTime? LastSubmissionDate { get; init; }
    public int EasySolved { get; init; }
    public int MediumSolved { get; init; }
    public int HardSolved { get; init; }
    public int? GlobalRank { get; init; }
    public int? CountryRank { get; init; }
}

// Rating History
public record RatingHistoryDto
{
    public long Id { get; init; }
    public long? ContestId { get; init; }
    public string? ContestName { get; init; }
    public int OldRating { get; init; }
    public int NewRating { get; init; }
    public int RatingChange { get; init; }
    public int? Rank { get; init; }
    public DateTime ChangedAt { get; init; }
}

// Achievement
public record AchievementDto
{
    public long Id { get; init; }
    public string AchievementType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public int Points { get; init; }
    public DateTime EarnedAt { get; init; }
}

// User Settings
public record UserSettingsDto
{
    public string LanguagePreference { get; init; } = "en";
    public string Theme { get; init; } = "light";
    public bool EmailNotifications { get; init; }
    public bool ContestReminders { get; init; }
    public string SolutionVisibility { get; init; } = "public";
    public bool ShowRating { get; init; }
    public string Timezone { get; init; } = "UTC";
}

public record UpdateUserSettingsRequest
{
    [ValidLanguagePreference]
    [StringLength(10)]
    public string? LanguagePreference { get; init; }

    [ValidTheme]
    [StringLength(20)]
    public string? Theme { get; init; }

    public bool? EmailNotifications { get; init; }

    public bool? ContestReminders { get; init; }

    [ValidSolutionVisibility]
    [StringLength(20)]
    public string? SolutionVisibility { get; init; }

    public bool? ShowRating { get; init; }

    [ValidTimezone]
    [StringLength(50)]
    public string? Timezone { get; init; }
}

// User List (for admin)
public record UserListItemDto
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public int Rating { get; init; }
    public bool IsVerified { get; init; }
    public bool IsBanned { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public List<string> Roles { get; init; } = new();
}

// Paginated Response
public record PaginatedResponse<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

// User Search/Filter
public record UserSearchRequest
{
    [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
    public string? SearchTerm { get; init; }

    [StringLength(2, MinimumLength = 2)]
    public string? Country { get; init; }

    [Range(0, 5000)]
    public int? MinRating { get; init; }

    [Range(0, 5000)]
    public int? MaxRating { get; init; }

    public bool? IsVerified { get; init; }

    public bool? IsBanned { get; init; }

    [StringLength(50)]
    public string? SortBy { get; init; } = "rating";

    [StringLength(4)]
    public string? SortOrder { get; init; } = "desc";

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    // Helper method to normalize empty strings to null and sanitize input
    public UserSearchRequest Normalize()
    {
        return this with
        {
            SearchTerm = SanitizeSearchTerm(SearchTerm),
            Country = string.IsNullOrWhiteSpace(Country) ? null : Country.Trim().ToUpperInvariant(),
            SortBy = string.IsNullOrWhiteSpace(SortBy) ? "rating" : SortBy.Trim().ToLowerInvariant(),
            SortOrder = string.IsNullOrWhiteSpace(SortOrder) ? "desc" : SortOrder.Trim().ToLowerInvariant()
        };
    }

    private static string? SanitizeSearchTerm(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        // Trim and remove control characters (including null bytes)
        var sanitized = new string(searchTerm
            .Trim()
            .Where(c => !char.IsControl(c))
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }
}

// Ban User
public record BanUserRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}

// Assign Role
public record AssignRoleRequest
{
    [Required]
    public long RoleId { get; init; }
}
