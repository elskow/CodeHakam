using AccountService.Models;

namespace AccountService.Tests.Helpers;

public static class TestDataBuilder
{
    public static User CreateTestUser(
        string username = "testuser",
        string email = "test@example.com",
        bool isVerified = true,
        bool isBanned = false,
        int rating = 1500)
    {
        return new User
        {
            Id = 1,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = isVerified,
            PasswordHash = "AQAAAAIAAYagAAAAEMockHashForTestingPurposesOnly",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            IsVerified = isVerified,
            IsBanned = isBanned,
            Rating = rating,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
    }

    public static RefreshToken CreateRefreshToken(
        long userId,
        string tokenHash,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null,
        string? ipAddress = null)
    {
        return new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt,
            CreatedByIp = ipAddress ?? "127.0.0.1"
        };
    }

    public static UserSettings CreateUserSettings(
        long userId,
        string language = "en",
        string theme = "light",
        string solutionVisibility = "public",
        string timezone = "UTC")
    {
        return new UserSettings
        {
            UserId = userId,
            LanguagePreference = language,
            Theme = theme,
            SolutionVisibility = solutionVisibility,
            Timezone = timezone,
            EmailNotifications = true,
            ContestReminders = true,
            ShowRating = true,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static UserStatistics CreateUserStatistics(
        long userId,
        int problemsSolved = 0,
        int contestsParticipated = 0,
        int totalSubmissions = 0)
    {
        return new UserStatistics
        {
            UserId = userId,
            ProblemsSolved = problemsSolved,
            ContestsParticipated = contestsParticipated,
            TotalSubmissions = totalSubmissions,
            AcceptedSubmissions = 0,
            AcceptanceRate = 0,
            MaxStreak = 0,
            CurrentStreak = 0,
            LastSubmissionDate = null,
            EasySolved = 0,
            MediumSolved = 0,
            HardSolved = 0,
            GlobalRank = null,
            CountryRank = null,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
