namespace AccountService.Models;

public sealed class Achievement
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string AchievementType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int Points { get; set; } = 0;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}
