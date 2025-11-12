namespace ContentService.Services.Implementations;

using System.Text;
using System.Text.Json;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly string _exchangeName;
    private bool _disposed;

    public RabbitMQEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMQEventPublisher> logger)
    {
        _logger = logger;

        var host = configuration["RabbitMQ:Host"] ?? "localhost";
        var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "codehakam.events";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogInformation("RabbitMQ connection established to {Host}:{Port}", host, port);
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        var messageBody = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var body = Encoding.UTF8.GetBytes(messageBody);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published event to {RoutingKey}: {MessageType}", routingKey, typeof(T).Name);

        await Task.CompletedTask;
    }

    public Task PublishProblemCreatedAsync(long problemId, string title, string slug, long authorId, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            ProblemId = problemId,
            Title = title,
            Slug = slug,
            AuthorId = authorId,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("problem.created", message, cancellationToken);
    }

    public Task PublishProblemUpdatedAsync(long problemId, string title, long updatedBy, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            ProblemId = problemId,
            Title = title,
            UpdatedBy = updatedBy,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("problem.updated", message, cancellationToken);
    }

    public Task PublishProblemDeletedAsync(long problemId, string title, long deletedBy, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            ProblemId = problemId,
            Title = title,
            DeletedBy = deletedBy,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("problem.deleted", message, cancellationToken);
    }

    public Task PublishTestCaseUploadedAsync(long problemId, long testCaseId, bool isSample, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            ProblemId = problemId,
            TestCaseId = testCaseId,
            IsSample = isSample,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("testcase.uploaded", message, cancellationToken);
    }

    public Task PublishEditorialPublishedAsync(long problemId, long editorialId, long authorId, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            ProblemId = problemId,
            EditorialId = editorialId,
            AuthorId = authorId,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("editorial.published", message, cancellationToken);
    }

    public Task PublishDiscussionCreatedAsync(long discussionId, long problemId, string title, long authorId, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            DiscussionId = discussionId,
            ProblemId = problemId,
            Title = title,
            AuthorId = authorId,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("discussion.created", message, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        _disposed = true;
        _logger.LogInformation("RabbitMQ connection closed");
    }
}
