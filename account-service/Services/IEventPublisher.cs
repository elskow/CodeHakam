using AccountService.Services.Impl;

namespace AccountService.Services;

public interface IEventPublisher
{
    Task PublishUserRegisteredAsync(UserRegisteredEvent eventData);
    Task PublishUserCreatedAsync(UserCreatedEvent eventData);
    Task PublishUserUpdatedAsync(UserUpdatedEvent eventData);
    Task PublishUserDeletedAsync(UserDeletedEvent eventData);
    Task PublishUserLoggedInAsync(UserLoggedInEvent eventData);
    Task PublishPasswordResetRequestedAsync(PasswordResetRequestedEvent eventData);
    Task PublishUserRatingChangedAsync(UserRatingChangedEvent eventData);
    Task PublishAchievementEarnedAsync(AchievementEarnedEvent eventData);
    Task PublishRoleAssignedAsync(RoleAssignedEvent eventData);
    Task PublishUserBannedAsync(UserBannedEvent eventData);
}
