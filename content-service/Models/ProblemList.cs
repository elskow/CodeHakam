namespace ContentService.Models;

public sealed class ProblemList
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public long OwnerId { get; set; }
    public long[] ProblemIds { get; set; } = Array.Empty<long>();
    public bool IsPublic { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
