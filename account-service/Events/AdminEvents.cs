namespace AccountService.Events;

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
