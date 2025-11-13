namespace AccountService.Events;

public record UserRegisteredEvent
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record UserLoggedInEvent
{
    public long UserId { get; init; }
    public string? IpAddress { get; init; }
    public DateTime Timestamp { get; init; }
}

public record PasswordResetRequestedEvent
{
    public long UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
