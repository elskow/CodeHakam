using ContentService.Enums;

namespace ContentService.Models;

public sealed class Problem
{
    public long Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string InputFormat { get; set; }
    public required string OutputFormat { get; set; }
    public required string Constraints { get; set; }
    public Difficulty Difficulty { get; set; }
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public long AuthorId { get; set; }
    public ProblemVisibility Visibility { get; set; }
    public string? HintText { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ViewCount { get; set; }
    public int SubmissionCount { get; set; }
    public int AcceptedCount { get; set; }
    public decimal AcceptanceRate { get; set; }

    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    public Editorial? Editorial { get; set; }
    public ICollection<ProblemTag> Tags { get; set; } = new List<ProblemTag>();
}
