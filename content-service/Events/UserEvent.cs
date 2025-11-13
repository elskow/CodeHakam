namespace ContentService.Events;

public class UserEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
