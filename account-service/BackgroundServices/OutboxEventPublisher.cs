using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountService.BackgroundServices;

public class OutboxEventPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 50;

    public OutboxEventPublisher(
        IServiceProvider serviceProvider,
        ILogger<OutboxEventPublisher> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
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
            .Where(e => e.Status == OutboxEventStatus.Pending ||
                       (e.Status == OutboxEventStatus.Failed && e.NextRetryAt <= DateTime.UtcNow))
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
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            var exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "codehakam.events";
            var exchangeType = _configuration["RabbitMQ:ExchangeType"] ?? "topic";

            channel.ExchangeDeclare(exchangeName, exchangeType, durable: true);

            foreach (var outboxEvent in pendingEvents)
            {
                try
                {
                    outboxEvent.Status = OutboxEventStatus.Processing;
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

                    outboxEvent.Status = OutboxEventStatus.Published;
                    outboxEvent.PublishedAt = DateTime.UtcNow;
                    outboxEvent.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Published outbox event {EventId} of type {EventType}",
                        outboxEvent.EventId, outboxEvent.EventType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox event {EventId}", outboxEvent.EventId);

                    outboxEvent.Status = OutboxEventStatus.Failed;
                    outboxEvent.RetryCount++;
                    outboxEvent.LastError = ex.Message.Length > 2000 ? ex.Message.Substring(0, 2000) : ex.Message;
                    outboxEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, Math.Min(outboxEvent.RetryCount, 6)));
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
