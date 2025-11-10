using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("user_settings")]
public sealed class UserSettings
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("language_preference")]
    [MaxLength(10)]
    public string LanguagePreference { get; set; } = "en";

    [Column("theme")]
    [MaxLength(20)]
    public string Theme { get; set; } = "light";

    [Column("email_notifications")]
    public bool EmailNotifications { get; set; } = true;

    [Column("contest_reminders")]
    public bool ContestReminders { get; set; } = true;

    [Column("solution_visibility")]
    [MaxLength(20)]
    public string SolutionVisibility { get; set; } = "public"; // public, private, friends

    [Column("show_rating")]
    public bool ShowRating { get; set; } = true;

    [Column("timezone")]
    [MaxLength(50)]
    public string Timezone { get; set; } = "UTC";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
