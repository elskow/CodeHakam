namespace ContentService.Models;

/// <summary>
/// Local cache of user profile data from account-service.
/// Updated via RabbitMQ events (user.created, user.updated, user.deleted).
/// </summary>
public sealed class UserProfile
{
    public long UserId { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
}
