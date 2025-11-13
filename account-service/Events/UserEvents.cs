namespace AccountService.Events;

public record UserCreatedEvent
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserUpdatedEvent
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UserDeletedEvent
{
    public long UserId { get; init; }
    public DateTime DeletedAt { get; init; }
}

public record UserRatingChangedEvent
{
    public long UserId { get; init; }
    public int OldRating { get; init; }
    public int NewRating { get; init; }
    public long? ContestId { get; init; }
    public DateTime Timestamp { get; init; }
}

public record AchievementEarnedEvent
{
    public long UserId { get; init; }
    public string AchievementType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
