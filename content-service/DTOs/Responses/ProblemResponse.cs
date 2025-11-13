namespace ContentService.DTOs.Responses;

public class ProblemResponse
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public required string InputFormat { get; set; }
    public required string OutputFormat { get; set; }
    public required string Constraints { get; set; }
    public required string Difficulty { get; set; }
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public long AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
    public string? Visibility { get; set; }
    public string? HintText { get; set; }
    public List<string> Tags { get; set; } = new();
    public int ViewCount { get; set; }
    public int SubmissionCount { get; set; }
    public int AcceptedCount { get; set; }
    public double AcceptanceRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
