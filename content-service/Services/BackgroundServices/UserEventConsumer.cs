using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ContentService.Data;
using ContentService.Events;
using ContentService.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ContentService.Services.BackgroundServices;

public class UserEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private const int MaxRetryCount = 5;

    public UserEventConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<UserEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var host = _configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");
            var username = _configuration["RabbitMQ:Username"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";
            var virtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";
            var exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "codehakam.events";

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password,
                VirtualHost = virtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange (idempotent)
            _channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare dead letter exchange
            var dlxName = $"{exchangeName}.dlx";
            _channel.ExchangeDeclare(dlxName, ExchangeType.Topic, durable: true);

            // Declare DLQ
            var dlqName = "content-service.user-events.dlq";
            _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(dlqName, dlxName, "#");

            // Declare queue for this service with DLX
            var queueName = "content-service.user-events";
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxName }
            };
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            // Bind queue to exchange with routing keys
            _channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: "user.created");
            _channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: "user.updated");
            _channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: "user.deleted");

            // Set QoS to process one message at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation(
                "UserEventConsumer connected to RabbitMQ at {Host}:{Port}, listening on queue: {QueueName}",
                host, port, queueName);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var routingKey = ea.RoutingKey;
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    _logger.LogInformation("Received event: {RoutingKey}, Message: {Message}", routingKey, message);

                    // Deserialize the event envelope (snake_case from account-service)
                    var envelope = JsonSerializer.Deserialize<EventEnvelope>(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Envelope deserialized: EventType={EventType}, EventId={EventId}, Data={Data}",
                        envelope?.EventType, envelope?.EventId, envelope?.Data.ToString());

                    if (envelope?.Data == null)
                    {
                        _logger.LogWarning("Failed to deserialize event envelope for {RoutingKey}", routingKey);
                        _channel.BasicNack(ea.DeliveryTag, false, false); // Don't requeue - send to DLQ
                        return;
                    }

                    _logger.LogInformation("Processing event {EventType} with ID {EventId}",
                        envelope.EventType, envelope.EventId);

                    // Check idempotency
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();

                    var alreadyProcessed = await dbContext.ProcessedEvents
                        .AnyAsync(pe => pe.EventId == envelope.EventId);

                    if (alreadyProcessed)
                    {
                        _logger.LogInformation("Event {EventId} already processed, skipping", envelope.EventId);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    // Deserialize the inner user event data (PascalCase from outbox)
                    var userEventJson = envelope.Data.GetRawText();
                    var userEvent = JsonSerializer.Deserialize<UserEvent>(userEventJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (userEvent == null)
                    {
                        _logger.LogWarning("Failed to deserialize user event data for {EventId}", envelope.EventId);
                        _channel.BasicNack(ea.DeliveryTag, false, false); // Don't requeue - send to DLQ
                        return;
                    }

                    // Process event with timing
                    var stopwatch = Stopwatch.StartNew();
                    await ProcessUserEventAsync(routingKey, userEvent, envelope.EventId, dbContext);
                    stopwatch.Stop();

                    // Record processed event
                    var processedEvent = new ProcessedEvent
                    {
                        EventId = envelope.EventId,
                        EventType = envelope.EventType,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDurationMs = stopwatch.ElapsedMilliseconds
                    };
                    dbContext.ProcessedEvents.Add(processedEvent);
                    await dbContext.SaveChangesAsync();

                    // Acknowledge successful processing
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation(
                        "Successfully processed event {EventId} in {Duration}ms",
                        envelope.EventId, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {RoutingKey}", routingKey);

                    // Check retry count from message headers
                    var retryCount = 0;
                    if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-death"))
                    {
                        var deaths = ea.BasicProperties.Headers["x-death"] as List<object>;
                        if (deaths != null && deaths.Count > 0)
                        {
                            var death = deaths[0] as Dictionary<string, object>;
                            if (death != null && death.ContainsKey("count"))
                            {
                                retryCount = Convert.ToInt32(death["count"]);
                            }
                        }
                    }

                    if (retryCount >= MaxRetryCount)
                    {
                        _logger.LogError("Max retries ({MaxRetries}) exceeded for event, sending to DLQ", MaxRetryCount);
                        _channel.BasicNack(ea.DeliveryTag, false, false); // Send to DLQ
                    }
                    else
                    {
                        _logger.LogWarning("Requeuing event for retry (attempt {RetryCount})", retryCount + 1);
                        _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
                    }
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("UserEventConsumer is now consuming messages");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the application is shutting down
            _logger.LogInformation("UserEventConsumer is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in UserEventConsumer");
            throw;
        }
    }

    private async Task ProcessUserEventAsync(string routingKey, UserEvent userEvent, string eventId, ContentDbContext dbContext)
    {
        switch (routingKey)
        {
            case "user.created":
            case "user.updated":
                await UpsertUserProfileAsync(dbContext, userEvent);
                break;

            case "user.deleted":
                await DeleteUserProfileAsync(dbContext, userEvent.UserId);
                break;

            default:
                _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task UpsertUserProfileAsync(ContentDbContext dbContext, UserEvent userEvent)
    {
        var existingProfile = await dbContext.UserProfiles.FindAsync(userEvent.UserId);

        if (existingProfile == null)
        {
            // Create new profile
            var newProfile = new UserProfile
            {
                UserId = userEvent.UserId,
                Username = userEvent.Username,
                DisplayName = userEvent.DisplayName,
                Email = userEvent.Email,
                AvatarUrl = userEvent.AvatarUrl,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.UserProfiles.Add(newProfile);
            _logger.LogInformation("Created user profile cache for user {UserId}", userEvent.UserId);
        }
        else
        {
            // Update existing profile
            existingProfile.Username = userEvent.Username;
            existingProfile.DisplayName = userEvent.DisplayName;
            existingProfile.Email = userEvent.Email;
            existingProfile.AvatarUrl = userEvent.AvatarUrl;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updated user profile cache for user {UserId}", userEvent.UserId);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task DeleteUserProfileAsync(ContentDbContext dbContext, long userId)
    {
        var profile = await dbContext.UserProfiles.FindAsync(userId);
        if (profile != null)
        {
            dbContext.UserProfiles.Remove(profile);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Deleted user profile cache for user {UserId}", userId);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

// Event envelope structure from account-service
public class EventEnvelope
{
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
    public DateTime Timestamp { get; set; }
}
