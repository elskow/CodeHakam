using AccountService.Models;

namespace AccountService.Tests.Helpers;

public static class TestDataBuilder
{
    public static User CreateTestUser(
        long? id = null,
        string username = "testuser",
        string email = "test@example.com",
        bool isVerified = true,
        bool isBanned = false,
        int rating = 1500)
    {
        var user = new User
        {
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

        if (id.HasValue)
        {
            user.Id = id.Value;
        }

        return user;
    }

    public static RefreshToken CreateRefreshToken(
        long userId,
        string tokenHash,
        long? id = null,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null,
        string? ipAddress = null)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt,
            CreatedByIp = ipAddress ?? "127.0.0.1"
        };

        if (id.HasValue)
        {
            token.Id = id.Value;
        }

        return token;
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

    public static Role CreateRole(
        long? id = null,
        string name = "User",
        string description = "Standard user role")
    {
        var role = new Role
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            role.Id = id.Value;
        }

        return role;
    }

    public static UserRole CreateUserRole(
        long userId,
        long roleId,
        Role? role = null)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            Role = role ?? CreateRole(roleId),
            AssignedAt = DateTime.UtcNow
        };
    }

    public static RatingHistory CreateRatingHistory(
        long userId,
        long contestId,
        long? id = null,
        int oldRating = 1500,
        int newRating = 1600,
        int? rank = null)
    {
        var history = new RatingHistory
        {
            UserId = userId,
            ContestId = contestId,
            OldRating = oldRating,
            NewRating = newRating,
            Rank = rank,
            ChangedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            history.Id = id.Value;
        }

        return history;
    }

    public static Achievement CreateAchievement(
        long userId,
        long? id = null,
        string achievementType = "first_solve",
        string title = "First Blood",
        string description = "Solved first problem",
        int points = 10)
    {
        var achievement = new Achievement
        {
            UserId = userId,
            AchievementType = achievementType,
            Title = title,
            Description = description,
            Points = points,
            EarnedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            achievement.Id = id.Value;
        }

        return achievement;
    }
}
