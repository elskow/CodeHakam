using System.Text.Json;
using AccountService.Constants;
using AccountService.Data;
using AccountService.Events;
using AccountService.Models;

using AccountService.Services.Interfaces;
namespace AccountService.Services.Implementations;

public sealed class OutboxEventPublisherService : IEventPublisher
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutboxEventPublisherService> _logger;

    public OutboxEventPublisherService(
        ApplicationDbContext context,
        ILogger<OutboxEventPublisherService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task PublishUserRegisteredAsync(UserRegisteredEvent eventData)
    {
        await SaveToOutboxAsync("user.registered", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishUserCreatedAsync(UserCreatedEvent eventData)
    {
        await SaveToOutboxAsync("user.created", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishUserUpdatedAsync(UserUpdatedEvent eventData)
    {
        await SaveToOutboxAsync("user.updated", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishUserDeletedAsync(UserDeletedEvent eventData)
    {
        await SaveToOutboxAsync("user.deleted", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishUserLoggedInAsync(UserLoggedInEvent eventData)
    {
        await SaveToOutboxAsync("user.logged_in", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishPasswordResetRequestedAsync(PasswordResetRequestedEvent eventData)
    {
        await SaveToOutboxAsync("user.password_reset_requested", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishUserRatingChangedAsync(UserRatingChangedEvent eventData)
    {
        await SaveToOutboxAsync("user.rating_changed", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishAchievementEarnedAsync(AchievementEarnedEvent eventData)
    {
        await SaveToOutboxAsync("user.achievement_earned", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishRoleAssignedAsync(RoleAssignedEvent eventData)
    {
        await SaveToOutboxAsync("user.role_assigned", eventData.UserId.ToString(), "User", eventData);
    }

    public async Task PublishUserBannedAsync(UserBannedEvent eventData)
    {
        await SaveToOutboxAsync("user.banned", eventData.UserId.ToString(), "User", eventData);
    }

    private Task SaveToOutboxAsync<T>(string eventType, string aggregateId, string aggregateType, T eventData)
    {
        try
        {
            var outboxEvent = new OutboxEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = eventType,
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                Payload = JsonSerializer.Serialize(eventData),
                CreatedAt = DateTime.UtcNow,
                Status = OutboxEventConstants.Pending
            };

            _context.OutboxEvents.Add(outboxEvent);

            _logger.LogInformation("Event {EventType} queued to outbox with ID {EventId}", eventType, outboxEvent.EventId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue event {EventType} to outbox", eventType);
            throw;
        }
    }
}
