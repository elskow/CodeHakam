using AccountService.Configuration;
using AccountService.Constants;
using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountService.Services.BackgroundServices;

public class OutboxEventPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventPublisher> _logger;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(ApplicationConstants.Intervals.OutboxPollingSeconds);
    private readonly int _batchSize = ApplicationConstants.Limits.OutboxBatchSize;

    public OutboxEventPublisher(
        IServiceProvider serviceProvider,
        ILogger<OutboxEventPublisher> logger,
        IOptions<RabbitMqSettings> rabbitMqSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _rabbitMqSettings = rabbitMqSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxEventPublisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxEventPublisher stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingEvents = await dbContext.OutboxEvents
            .Where(e => e.Status == OutboxEventConstants.Pending ||
                       (e.Status == OutboxEventConstants.Failed && e.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(stoppingToken);

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox events", pendingEvents.Count);

        IConnection? connection = null;
        IModel? channel = null;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqSettings.Host,
                Port = _rabbitMqSettings.Port,
                UserName = _rabbitMqSettings.Username,
                Password = _rabbitMqSettings.Password,
                VirtualHost = _rabbitMqSettings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(ApplicationConstants.Intervals.NetworkRecoverySeconds)
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            var exchangeName = _rabbitMqSettings.ExchangeName;
            var exchangeType = "topic";

            channel.ExchangeDeclare(exchangeName, exchangeType, durable: true);

            foreach (var outboxEvent in pendingEvents)
            {
                try
                {
                    outboxEvent.Status = OutboxEventConstants.Processing;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    // Wrap payload in EventEnvelope structure
                    // First parse the payload as JsonDocument to convert to snake_case
                    using var payloadDoc = JsonDocument.Parse(outboxEvent.Payload);

                    var envelope = new
                    {
                        event_type = outboxEvent.EventType,
                        event_id = outboxEvent.EventId,
                        data = payloadDoc.RootElement,
                        timestamp = outboxEvent.CreatedAt
                    };

                    var envelopeJson = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = outboxEvent.EventId;
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.ContentType = "application/json";
                    properties.Headers = new Dictionary<string, object>
                    {
                        { "event-type", outboxEvent.EventType },
                        { "aggregate-id", outboxEvent.AggregateId },
                        { "aggregate-type", outboxEvent.AggregateType }
                    };

                    var body = Encoding.UTF8.GetBytes(envelopeJson);
                    channel.BasicPublish(
                        exchange: exchangeName,
                        routingKey: outboxEvent.EventType,
                        basicProperties: properties,
                        body: body
                    );

                    outboxEvent.Status = OutboxEventConstants.Published;
                    outboxEvent.PublishedAt = DateTime.UtcNow;
                    outboxEvent.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Published outbox event {EventId} of type {EventType}",
                        outboxEvent.EventId, outboxEvent.EventType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox event {EventId}", outboxEvent.EventId);

                    outboxEvent.Status = OutboxEventConstants.Failed;
                    outboxEvent.RetryCount++;
                    outboxEvent.LastError = ex.Message.Length > ApplicationConstants.Limits.MaxErrorMessageLength
                        ? ex.Message.Substring(0, ApplicationConstants.Limits.MaxErrorMessageLength)
                        : ex.Message;
                    outboxEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, Math.Min(outboxEvent.RetryCount, ApplicationConstants.Limits.MaxRetryExponent)));
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
        }
        finally
        {
            channel?.Close();
            connection?.Close();
        }
    }
}
