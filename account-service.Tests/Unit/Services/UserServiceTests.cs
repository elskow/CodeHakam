using AccountService.Data;
using AccountService.DTOs;
using AccountService.Models;
using AccountService.Services.Impl;
using AccountService.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.Unit.Services;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"UserServiceTests_{Guid.NewGuid()}");
        _loggerMock = new Mock<ILogger<UserService>>();
        _userService = new UserService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetUserProfileAsync_WithExistingUser_ShouldReturnProfile()
    {
        var user = TestDataBuilder.CreateTestUser(
            username: "johndoe",
            email: "john@example.com",
            rating: 1800);
        var role = new Role { Name = "User" };

        await _context.Users.AddAsync(user);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        var statistics = TestDataBuilder.CreateUserStatistics(
            userId: user.Id,
            problemsSolved: 50,
            contestsParticipated: 10,
            totalSubmissions: 100);
        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id, Role = role };

        await _context.UserStatistics.AddAsync(statistics);
        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        var result = await _userService.GetUserProfileAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be("johndoe");
        result.Email.Should().Be("john@example.com");
        result.Rating.Should().Be(1800);
        result.Statistics.Should().NotBeNull();
        result.Statistics!.ProblemsSolved.Should().Be(50);
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNonExistentUser_ShouldReturnNull()
    {
        var result = await _userService.GetUserProfileAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_ShouldReturnCurrentUserProfile()
    {
        var user = TestDataBuilder.CreateTestUser(username: "currentuser");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _userService.GetCurrentUserProfileAsync(user.Id);

        result.Should().NotBeNull();
        result!.Username.Should().Be("currentuser");
    }

    [Fact]
    public async Task GetPublicUserProfileAsync_WithValidUser_ShouldReturnProfileWithoutEmail()
    {
        var user = TestDataBuilder.CreateTestUser(
            username: "publicuser",
            email: "private@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _userService.GetPublicUserProfileAsync(user.Id);

        result.Should().NotBeNull();
        result!.Username.Should().Be("publicuser");
        result.Email.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublicUserProfileAsync_WithBannedUser_ShouldReturnNull()
    {
        var user = TestDataBuilder.CreateTestUser(isBanned: true);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _userService.GetPublicUserProfileAsync(user.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublicUserProfileAsync_WithNonExistentUser_ShouldReturnNull()
    {
        var result = await _userService.GetPublicUserProfileAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdateAllFields()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            FullName = "John Doe",
            Bio = "Software engineer and competitive programmer",
            Country = "US",
            Organization = "Tech Corp",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        var result = await _userService.UpdateUserProfileAsync(user.Id, request);

        result.Should().NotBeNull();
        result.FullName.Should().Be("John Doe");
        result.Bio.Should().Be("Software engineer and competitive programmer");
        result.Country.Should().Be("US");
        result.Organization.Should().Be("Tech Corp");

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FullName.Should().Be("John Doe");
        updatedUser.Bio.Should().Be("Software engineer and competitive programmer");
        updatedUser.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        var user = TestDataBuilder.CreateTestUser();
        user.FullName = "Original Name";
        user.Bio = "Original Bio";
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            FullName = "Updated Name",
            Bio = null,
            Country = null
        };

        var result = await _userService.UpdateUserProfileAsync(user.Id, request);

        result.FullName.Should().Be("Updated Name");
        result.Bio.Should().Be("Original Bio");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithNonExistentUser_ShouldThrowException()
    {
        var request = new UpdateProfileRequest { FullName = "Test" };

        var act = async () => await _userService.UpdateUserProfileAsync(999, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task GetUserStatisticsAsync_WithExistingStatistics_ShouldReturnStatistics()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var statistics = TestDataBuilder.CreateUserStatistics(
            userId: user.Id,
            problemsSolved: 75,
            contestsParticipated: 20,
            totalSubmissions: 200);
        statistics.AcceptedSubmissions = 150;

        await _context.UserStatistics.AddAsync(statistics);
        await _context.SaveChangesAsync();

        var result = await _userService.GetUserStatisticsAsync(user.Id);

        result.Should().NotBeNull();
        result!.ProblemsSolved.Should().Be(75);
        result.ContestsParticipated.Should().Be(20);
        result.TotalSubmissions.Should().Be(200);
        result.AcceptedSubmissions.Should().Be(150);
        result.AcceptanceRate.Should().Be(75.00m);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_WithZeroSubmissions_ShouldReturnZeroAcceptanceRate()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var statistics = TestDataBuilder.CreateUserStatistics(userId: user.Id);

        await _context.UserStatistics.AddAsync(statistics);
        await _context.SaveChangesAsync();

        var result = await _userService.GetUserStatisticsAsync(user.Id);

        result.Should().NotBeNull();
        result!.AcceptanceRate.Should().Be(0);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_WithNonExistentUser_ShouldReturnNull()
    {
        var result = await _userService.GetUserStatisticsAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRatingHistoryAsync_ShouldReturnOrderedHistory()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);

        var history1 = new RatingHistory
        {
            UserId = user.Id,
            ContestId = 101,
            OldRating = 1500,
            NewRating = 1600,
            Rank = 15,
            ChangedAt = DateTime.UtcNow.AddDays(-2)
        };
        var history2 = new RatingHistory
        {
            UserId = user.Id,
            ContestId = 102,
            OldRating = 1600,
            NewRating = 1550,
            Rank = 25,
            ChangedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _context.RatingHistory.AddRangeAsync(history1, history2);
        await _context.SaveChangesAsync();

        var result = await _userService.GetRatingHistoryAsync(user.Id);

        result.Should().HaveCount(2);
        result[0].NewRating.Should().Be(1550);
        result[0].RatingChange.Should().Be(-50);
        result[1].NewRating.Should().Be(1600);
        result[1].RatingChange.Should().Be(100);
    }

    [Fact]
    public async Task GetRatingHistoryAsync_WithLimit_ShouldReturnLimitedResults()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);

        for (int i = 0; i < 10; i++)
        {
            var history = new RatingHistory
            {
                UserId = user.Id,
                ContestId = 100 + i,
                OldRating = 1500,
                NewRating = 1500 + i * 10,
                ChangedAt = DateTime.UtcNow.AddDays(-i)
            };
            await _context.RatingHistory.AddAsync(history);
        }
        await _context.SaveChangesAsync();

        var result = await _userService.GetRatingHistoryAsync(user.Id, limit: 5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnOrderedAchievements()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);

        var achievement1 = new Achievement
        {
            UserId = user.Id,
            AchievementType = "first_solve",
            Title = "First Blood",
            Description = "Solved first problem",
            Points = 10,
            EarnedAt = DateTime.UtcNow.AddDays(-5)
        };
        var achievement2 = new Achievement
        {
            UserId = user.Id,
            AchievementType = "problem_solver",
            Title = "Problem Solver",
            Description = "Solved 100 problems",
            Points = 100,
            EarnedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _context.Achievements.AddRangeAsync(achievement1, achievement2);
        await _context.SaveChangesAsync();

        var result = await _userService.GetUserAchievementsAsync(user.Id);

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Problem Solver");
        result[1].Title.Should().Be("First Blood");
    }

    [Fact]
    public async Task GetUserSettingsAsync_WithExistingSettings_ShouldReturnSettings()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var settings = TestDataBuilder.CreateUserSettings(
            userId: user.Id,
            language: "en",
            theme: "dark",
            timezone: "America/New_York");

        await _context.UserSettings.AddAsync(settings);
        await _context.SaveChangesAsync();

        var result = await _userService.GetUserSettingsAsync(user.Id);

        result.Should().NotBeNull();
        result!.LanguagePreference.Should().Be("en");
        result.Theme.Should().Be("dark");
        result.Timezone.Should().Be("America/New_York");
        result.EmailNotifications.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSettingsAsync_WithNonExistentSettings_ShouldReturnNull()
    {
        var result = await _userService.GetUserSettingsAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserSettingsAsync_WithExistingSettings_ShouldUpdateNormalizedValues()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var settings = TestDataBuilder.CreateUserSettings(userId: user.Id);

        await _context.UserSettings.AddAsync(settings);
        await _context.SaveChangesAsync();

        var request = new UpdateUserSettingsRequest
        {
            LanguagePreference = "FR",
            Theme = "DARK",
            SolutionVisibility = "PRIVATE",
            EmailNotifications = false,
            Timezone = "Europe/Paris"
        };

        var result = await _userService.UpdateUserSettingsAsync(user.Id, request);

        result.Should().NotBeNull();
        result.LanguagePreference.Should().Be("fr");
        result.Theme.Should().Be("dark");
        result.SolutionVisibility.Should().Be("private");
        result.EmailNotifications.Should().BeFalse();

        var updatedSettings = await _context.UserSettings.FindAsync(user.Id);
        updatedSettings!.LanguagePreference.Should().Be("fr");
        updatedSettings.Theme.Should().Be("dark");
        updatedSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateUserSettingsAsync_WithNonExistentSettings_ShouldCreateNewSettings()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserSettingsRequest
        {
            Theme = "dark",
            LanguagePreference = "es"
        };

        var result = await _userService.UpdateUserSettingsAsync(user.Id, request);

        result.Should().NotBeNull();
        result.Theme.Should().Be("dark");
        result.LanguagePreference.Should().Be("es");

        var settings = await _context.UserSettings.FindAsync(user.Id);
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserSettingsAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var settings = TestDataBuilder.CreateUserSettings(
            userId: user.Id,
            language: "en",
            theme: "light");

        await _context.UserSettings.AddAsync(settings);
        await _context.SaveChangesAsync();

        var request = new UpdateUserSettingsRequest
        {
            Theme = "dark"
        };

        var result = await _userService.UpdateUserSettingsAsync(user.Id, request);

        result.Theme.Should().Be("dark");
        result.LanguagePreference.Should().Be("en");
    }

    [Fact]
    public async Task SearchUsersAsync_WithNoFilters_ShouldReturnAllUsers()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "alice", rating: 1800);
        var user2 = TestDataBuilder.CreateTestUser(username: "bob", email: "bob@example.com", rating: 1600);

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest { Page = 1, PageSize = 10 };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.Items[0].Rating.Should().Be(1800);
    }

    [Fact]
    public async Task SearchUsersAsync_WithSearchTerm_ShouldFilterByUsernameEmailOrFullName()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "alice", email: "alice@example.com");
        user1.FullName = "Alice Smith";
        var user2 = TestDataBuilder.CreateTestUser(username: "bob", email: "bob@example.com");
        user2.FullName = "Bob Jones";

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            SearchTerm = "alice",
            Page = 1,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(1);
        result.Items[0].Username.Should().Be("alice");
    }

    [Fact]
    public async Task SearchUsersAsync_WithCountryFilter_ShouldFilterByCountry()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "user1");
        user1.Country = "US";
        var user2 = TestDataBuilder.CreateTestUser(username: "user2", email: "user2@example.com");
        user2.Country = "CA";

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            Country = "US",
            Page = 1,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(1);
        result.Items[0].Username.Should().Be("user1");
    }

    [Fact]
    public async Task SearchUsersAsync_WithRatingRange_ShouldFilterByRating()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "user1", rating: 1200);
        var user2 = TestDataBuilder.CreateTestUser(username: "user2", email: "user2@example.com", rating: 1800);
        var user3 = TestDataBuilder.CreateTestUser(username: "user3", email: "user3@example.com", rating: 2200);

        await _context.Users.AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            MinRating = 1500,
            MaxRating = 2000,
            Page = 1,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(1);
        result.Items[0].Rating.Should().Be(1800);
    }

    [Fact]
    public async Task SearchUsersAsync_WithVerifiedFilter_ShouldFilterByVerificationStatus()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "verified", isVerified: true);
        var user2 = TestDataBuilder.CreateTestUser(username: "unverified", email: "unverified@example.com", isVerified: false);

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            IsVerified = true,
            Page = 1,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(1);
        result.Items[0].IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task SearchUsersAsync_WithBannedFilter_ShouldFilterByBanStatus()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "active", isBanned: false);
        var user2 = TestDataBuilder.CreateTestUser(username: "banned", email: "banned@example.com", isBanned: true);

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            IsBanned = false,
            Page = 1,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(1);
        result.Items[0].IsBanned.Should().BeFalse();
    }

    [Fact]
    public async Task SearchUsersAsync_WithSortByUsername_ShouldSortCorrectly()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "charlie");
        var user2 = TestDataBuilder.CreateTestUser(username: "alice", email: "alice@example.com");
        var user3 = TestDataBuilder.CreateTestUser(username: "bob", email: "bob@example.com");

        await _context.Users.AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            SortBy = "username",
            SortOrder = "asc",
            Page = 1,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(3);
        result.Items[0].Username.Should().Be("alice");
        result.Items[1].Username.Should().Be("bob");
        result.Items[2].Username.Should().Be("charlie");
    }

    [Fact]
    public async Task SearchUsersAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (int i = 1; i <= 25; i++)
        {
            var user = TestDataBuilder.CreateTestUser(
                username: $"user{i}",
                email: $"user{i}@example.com");
            await _context.Users.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            Page = 2,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task SearchUsersAsync_WithLastPage_ShouldReturnRemainingItems()
    {
        for (int i = 1; i <= 25; i++)
        {
            var user = TestDataBuilder.CreateTestUser(
                username: $"user{i}",
                email: $"user{i}@example.com");
            await _context.Users.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest
        {
            Page = 3,
            PageSize = 10
        };

        var result = await _userService.SearchUsersAsync(request);

        result.Items.Should().HaveCount(5);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task UserExistsAsync_WithExistingUser_ShouldReturnTrue()
    {
        var user = TestDataBuilder.CreateTestUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _userService.UserExistsAsync(user.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserExistsAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        var result = await _userService.UserExistsAsync(999);

        result.Should().BeFalse();
    }
}
