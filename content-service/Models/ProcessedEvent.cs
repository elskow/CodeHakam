using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentService.Models;

[Table("processed_events")]
public sealed class ProcessedEvent
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
    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    [Column("processing_duration_ms")]
    public long ProcessingDurationMs { get; set; }
}
