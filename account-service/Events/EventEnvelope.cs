namespace AccountService.Events;

public record EventEnvelope<T>
{
    public string EventType { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public T Data { get; init; } = default!;
    public DateTime Timestamp { get; init; }
}
