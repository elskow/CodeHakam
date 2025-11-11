using AccountService.DTOs;

namespace AccountService.Services;

public interface IUserService
{
    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(long userId);

    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    Task<UserProfileDto?> GetCurrentUserProfileAsync(long currentUserId);

    /// <summary>
    /// Update user profile
    /// </summary>
    Task<UserProfileDto> UpdateUserProfileAsync(long userId, UpdateProfileRequest request);

    /// <summary>
    /// Get user statistics
    /// </summary>
    Task<UserStatisticsDto?> GetUserStatisticsAsync(long userId);

    /// <summary>
    /// Get user rating history
    /// </summary>
    Task<List<RatingHistoryDto>> GetRatingHistoryAsync(long userId, int limit = 50);

    /// <summary>
    /// Get user achievements
    /// </summary>
    Task<List<AchievementDto>> GetUserAchievementsAsync(long userId);

    /// <summary>
    /// Get user settings
    /// </summary>
    Task<UserSettingsDto?> GetUserSettingsAsync(long userId);

    /// <summary>
    /// Update user settings
    /// </summary>
    Task<UserSettingsDto> UpdateUserSettingsAsync(long userId, UpdateUserSettingsRequest request);

    /// <summary>
    /// Search users with pagination
    /// </summary>
    Task<PaginatedResponse<UserListItemDto>> SearchUsersAsync(UserSearchRequest request);

    /// <summary>
    /// Check if user exists by ID
    /// </summary>
    Task<bool> UserExistsAsync(long userId);

    /// <summary>
    /// Get public user profile (without sensitive information)
    /// </summary>
    Task<UserProfileDto?> GetPublicUserProfileAsync(long userId);
}
