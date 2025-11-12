namespace ContentService.Models;

public sealed class ProblemTag
{
    public long ProblemId { get; set; }
    public required string Tag { get; set; }
    public DateTime CreatedAt { get; set; }

    public Problem Problem { get; set; } = null!;
}
