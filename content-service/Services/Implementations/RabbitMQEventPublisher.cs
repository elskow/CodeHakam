using System.Text;
using System.Text.Json;
using ContentService.Constants;
using ContentService.Services.Interfaces;
using RabbitMQ.Client;

namespace ContentService.Services.Implementations;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private bool _disposed;

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;

        var host = configuration["RabbitMQ:Host"] ?? "localhost";
        var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "codehakam.events";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            VirtualHost = virtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(ApplicationConstants.Intervals.NetworkRecoverySeconds)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            _exchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogInformation("RabbitMQ connection established to {Host}:{Port}", host, port);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _channel.Close();
        _channel.Dispose();
        _connection.Close();
        _connection.Dispose();

        _disposed = true;
        _logger.LogInformation("RabbitMQ connection closed");
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
            _exchangeName,
            routingKey,
            properties,
            body);

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

    public Task PublishTestCaseUploadedAsync(long testCaseId, long problemId, int testNumber, bool isSample, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            TestCaseId = testCaseId,
            ProblemId = problemId,
            TestNumber = testNumber,
            IsSample = isSample,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("testcase.uploaded", message, cancellationToken);
    }

    public Task PublishTestCaseDeletedAsync(long testCaseId, long problemId, int testNumber, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            TestCaseId = testCaseId,
            ProblemId = problemId,
            TestNumber = testNumber,
            Timestamp = DateTime.UtcNow
        };

        return PublishAsync("testcase.deleted", message, cancellationToken);
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
}
