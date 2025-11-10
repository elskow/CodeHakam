using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("rating_history", Schema = "users")]
public sealed class RatingHistory
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("contest_id")]
    public long? ContestId { get; set; }

    [Required]
    [Column("old_rating")]
    public int OldRating { get; set; }

    [Required]
    [Column("new_rating")]
    public int NewRating { get; set; }

    [Column("rank")]
    public int? Rank { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
