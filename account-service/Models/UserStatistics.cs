using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("user_statistics")]
public sealed class UserStatistics
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("problems_solved")]
    public int ProblemsSolved { get; set; } = 0;

    [Column("contests_participated")]
    public int ContestsParticipated { get; set; } = 0;

    [Column("total_submissions")]
    public int TotalSubmissions { get; set; } = 0;

    [Column("accepted_submissions")]
    public int AcceptedSubmissions { get; set; } = 0;

    [Column("acceptance_rate")]
    public decimal AcceptanceRate { get; set; } = 0;

    [Column("max_streak")]
    public int MaxStreak { get; set; } = 0;

    [Column("current_streak")]
    public int CurrentStreak { get; set; } = 0;

    [Column("last_submission_date")]
    public DateTime? LastSubmissionDate { get; set; }

    [Column("easy_solved")]
    public int EasySolved { get; set; } = 0;

    [Column("medium_solved")]
    public int MediumSolved { get; set; } = 0;

    [Column("hard_solved")]
    public int HardSolved { get; set; } = 0;

    [Column("global_rank")]
    public int? GlobalRank { get; set; }

    [Column("country_rank")]
    public int? CountryRank { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
