using System.Text;
using System.Text.Json;
using AccountService.Configuration;
using AccountService.Constants;
using AccountService.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

using AccountService.Services.Interfaces;
namespace AccountService.Services.Implementations;

public class EventPublisher : IEventPublisher, IDisposable
{
    private readonly Lock _lock = new();
    private readonly ILogger<EventPublisher> _logger;
    private readonly RabbitMqSettings _settings;
    private IModel? _channel;
    private IConnection? _connection;

    public EventPublisher(IOptions<RabbitMqSettings> settings, ILogger<EventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        InitializeRabbitMq();
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
                NetworkRecoveryInterval = TimeSpan.FromSeconds(ApplicationConstants.Intervals.NetworkRecoverySeconds)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                _settings.ExchangeName,
                ExchangeType.Topic,
                _settings.Durable,
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
                        _settings.ExchangeName,
                        routingKey,
                        properties,
                        body
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
}
