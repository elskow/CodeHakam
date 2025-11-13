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

public class UserEventConsumer(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<UserEventConsumer> logger)
    : BackgroundService
{
    private const int MaxRetryCount = 5;
    private IModel? _channel;
    private IConnection? _connection;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
            var username = configuration["RabbitMQ:Username"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";
            var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
            var exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "codehakam.events";

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
                exchangeName,
                ExchangeType.Topic,
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
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                queueArgs);

            // Bind queue to exchange with routing keys
            _channel.QueueBind(
                queueName,
                exchangeName,
                "user.created");
            _channel.QueueBind(
                queueName,
                exchangeName,
                "user.updated");
            _channel.QueueBind(
                queueName,
                exchangeName,
                "user.deleted");

            // Set QoS to process one message at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            logger.LogInformation(
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
                    logger.LogInformation("Received event: {RoutingKey}, Message: {Message}", routingKey, message);

                    // Deserialize the event envelope (snake_case from account-service)
                    var envelope = JsonSerializer.Deserialize<EventEnvelope>(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        PropertyNameCaseInsensitive = true
                    });

                    logger.LogInformation("Envelope deserialized: EventType={EventType}, EventId={EventId}, Data={Data}",
                        envelope?.EventType, envelope?.EventId, envelope?.Data.ToString());

                    if (envelope?.Data == null)
                    {
                        logger.LogWarning("Failed to deserialize event envelope for {RoutingKey}", routingKey);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // Don't requeue - send to DLQ
                        return;
                    }

                    logger.LogInformation("Processing event {EventType} with ID {EventId}",
                        envelope.EventType, envelope.EventId);

                    // Check idempotency
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();

                    var alreadyProcessed = await dbContext.ProcessedEvents
                        .AnyAsync(pe => pe.EventId == envelope.EventId, stoppingToken);

                    if (alreadyProcessed)
                    {
                        logger.LogInformation("Event {EventId} already processed, skipping", envelope.EventId);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
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
                        logger.LogWarning("Failed to deserialize user event data for {EventId}", envelope.EventId);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // Don't requeue - send to DLQ
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
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    logger.LogInformation(
                        "Successfully processed event {EventId} in {Duration}ms",
                        envelope.EventId, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing event {RoutingKey}", routingKey);

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
                        logger.LogError("Max retries ({MaxRetries}) exceeded for event, sending to DLQ", MaxRetryCount);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // Send to DLQ
                    }
                    else
                    {
                        logger.LogWarning("Re-queuing event for retry (attempt {RetryCount})", retryCount + 1);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true); // Requeue
                    }
                }
            };

            _channel.BasicConsume(
                queueName,
                autoAck: false,
                consumer);

            logger.LogInformation("UserEventConsumer is now consuming messages");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the application is shutting down
            logger.LogInformation("UserEventConsumer is shutting down");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in UserEventConsumer");
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
                logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
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
            logger.LogInformation("Created user profile cache for user {UserId}", userEvent.UserId);
        }
        else
        {
            // Update existing profile
            existingProfile.Username = userEvent.Username;
            existingProfile.DisplayName = userEvent.DisplayName;
            existingProfile.Email = userEvent.Email;
            existingProfile.AvatarUrl = userEvent.AvatarUrl;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            logger.LogInformation("Updated user profile cache for user {UserId}", userEvent.UserId);
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
            logger.LogInformation("Deleted user profile cache for user {UserId}", userId);
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
