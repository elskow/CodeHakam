using System.ComponentModel.DataAnnotations;
using AccountService.Validators;

namespace AccountService.DTOs;

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
}
