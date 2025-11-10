using Microsoft.AspNetCore.Identity;

namespace AccountService.Models;

public sealed class User : IdentityUser<long>
{
    public int Rating { get; set; } = 1500;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Country { get; set; }
    public string? Organization { get; set; }
    public bool IsVerified { get; set; }
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Navigation properties
    public UserStatistics? Statistics { get; set; }
    public UserSettings? Settings { get; set; }
    public ICollection<RatingHistory> RatingHistory { get; set; } = new List<RatingHistory>();
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
