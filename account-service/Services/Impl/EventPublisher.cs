using System.Text;
using System.Text.Json;
using AccountService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AccountService.Services.Impl;

public class EventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<EventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly Lock _lock = new();

    public EventPublisher(IOptions<RabbitMqSettings> settings, ILogger<EventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: _settings.Durable,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
        }
    }

    public async Task PublishUserRegisteredAsync(UserRegisteredEvent eventData)
    {
        await PublishEventAsync("user.registered", eventData);
    }

    public async Task PublishUserCreatedAsync(UserCreatedEvent eventData)
    {
        await PublishEventAsync("user.created", eventData);
    }

    public async Task PublishUserUpdatedAsync(UserUpdatedEvent eventData)
    {
        await PublishEventAsync("user.updated", eventData);
    }

    public async Task PublishUserDeletedAsync(UserDeletedEvent eventData)
    {
        await PublishEventAsync("user.deleted", eventData);
    }

    public async Task PublishUserLoggedInAsync(UserLoggedInEvent eventData)
    {
        await PublishEventAsync("user.logged_in", eventData);
    }

    public async Task PublishPasswordResetRequestedAsync(PasswordResetRequestedEvent eventData)
    {
        await PublishEventAsync("user.password_reset_requested", eventData);
    }

    public async Task PublishUserRatingChangedAsync(UserRatingChangedEvent eventData)
    {
        await PublishEventAsync("user.rating_changed", eventData);
    }

    public async Task PublishAchievementEarnedAsync(AchievementEarnedEvent eventData)
    {
        await PublishEventAsync("user.achievement_earned", eventData);
    }

    public async Task PublishRoleAssignedAsync(RoleAssignedEvent eventData)
    {
        await PublishEventAsync("user.role_assigned", eventData);
    }

    public async Task PublishUserBannedAsync(UserBannedEvent eventData)
    {
        await PublishEventAsync("user.banned", eventData);
    }

    private Task PublishEventAsync<T>(string routingKey, T eventData)
    {
        return Task.Run(() =>
        {
            try
            {
                lock (_lock)
                {
                    if (_channel == null || _channel.IsClosed)
                    {
                        _logger.LogWarning("RabbitMQ channel is closed, attempting to reconnect");
                        InitializeRabbitMq();
                    }

                    if (_channel == null)
                    {
                        _logger.LogError("Failed to publish event: RabbitMQ channel is null");
                        return;
                    }

                    var envelope = new EventEnvelope<T>
                    {
                        EventType = routingKey,
                        Data = eventData,
                        Timestamp = DateTime.UtcNow,
                        EventId = Guid.NewGuid().ToString()
                    };

                    var message = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    var body = Encoding.UTF8.GetBytes(message);

                    var properties = _channel.CreateBasicProperties();
                    properties.ContentType = "application/json";
                    properties.DeliveryMode = 2; // Persistent
                    properties.MessageId = envelope.EventId;
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    _channel.BasicPublish(
                        exchange: _settings.ExchangeName,
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body
                    );

                    _logger.LogDebug("Published event {EventType} with ID {EventId}", routingKey, envelope.EventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event {EventType}", routingKey);
            }
        });
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}

// Event envelope for consistent message structure
public record EventEnvelope<T>
{
    public string EventType { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public T Data { get; init; } = default!;
    public DateTime Timestamp { get; init; }
}

// User lifecycle events for cross-service data synchronization
public record UserCreatedEvent
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserUpdatedEvent
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UserDeletedEvent
{
    public long UserId { get; init; }
    public DateTime DeletedAt { get; init; }
}

// Additional event types
public record UserRatingChangedEvent
{
    public long UserId { get; init; }
    public int OldRating { get; init; }
    public int NewRating { get; init; }
    public long? ContestId { get; init; }
    public DateTime Timestamp { get; init; }
}

public record AchievementEarnedEvent
{
    public long UserId { get; init; }
    public string AchievementType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record RoleAssignedEvent
{
    public long UserId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public long AssignedBy { get; init; }
    public DateTime Timestamp { get; init; }
}

public record UserBannedEvent
{
    public long UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public long BannedBy { get; init; }
    public DateTime Timestamp { get; init; }
}
