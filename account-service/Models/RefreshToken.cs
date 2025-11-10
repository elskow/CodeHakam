using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("refresh_tokens")]
public sealed class RefreshToken
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("user_id")]
    public long UserId { get; set; }

    [Required]
    [Column("token_hash")]
    [MaxLength(256)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("replaced_by_token")]
    [MaxLength(256)]
    public string? ReplacedByToken { get; set; }

    [Column("created_by_ip")]
    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    [Column("revoked_by_ip")]
    [MaxLength(45)]
    public string? RevokedByIp { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsRevoked => RevokedAt != null;

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}
