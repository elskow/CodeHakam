namespace ContentService.Models;

public sealed class Editorial
{
    public long Id { get; set; }
    public long ProblemId { get; set; }
    public required string Content { get; set; }
    public required string Approach { get; set; }
    public required string TimeComplexity { get; set; }
    public required string SpaceComplexity { get; set; }
    public string? SolutionCode { get; set; }
    public long AuthorId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Problem Problem { get; set; } = null!;
}
