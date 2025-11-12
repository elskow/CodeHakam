namespace ContentService.DTOs.Responses;

public class EditorialResponse
{
    public long Id { get; set; }
    public long ProblemId { get; set; }
    public long AuthorId { get; set; }
    public required string Content { get; set; }
    public required string TimeComplexity { get; set; }
    public required string SpaceComplexity { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
