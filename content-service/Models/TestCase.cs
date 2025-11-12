namespace ContentService.Models;

public sealed class TestCase
{
    public long Id { get; set; }
    public long ProblemId { get; set; }
    public int TestNumber { get; set; }
    public bool IsSample { get; set; }
    public required string InputFileUrl { get; set; }
    public required string OutputFileUrl { get; set; }
    public long InputSize { get; set; }
    public long OutputSize { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Problem Problem { get; set; } = null!;
}
