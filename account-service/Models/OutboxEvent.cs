using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("outbox_events")]
public class OutboxEvent
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("event_id")]
    [MaxLength(100)]
    public string EventId { get; set; } = string.Empty;

    [Required]
    [Column("event_type")]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("aggregate_id")]
    [MaxLength(100)]
    public string AggregateId { get; set; } = string.Empty;

    [Required]
    [Column("aggregate_type")]
    [MaxLength(100)]
    public string AggregateType { get; set; } = string.Empty;

    [Required]
    [Column("payload")]
    [MaxLength(10000)]
    public string Payload { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = OutboxEventStatus.Pending;

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    [Column("last_error")]
    [MaxLength(2000)]
    public string? LastError { get; set; }

    [Column("next_retry_at")]
    public DateTime? NextRetryAt { get; set; }
}

public static class OutboxEventStatus
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Published = "published";
    public const string Failed = "failed";
}
