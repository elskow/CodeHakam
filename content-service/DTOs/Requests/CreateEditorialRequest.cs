namespace ContentService.DTOs.Requests;

public class CreateEditorialRequest
{
    public required long ProblemId { get; set; }
    public required string Content { get; set; }
    public required string TimeComplexity { get; set; }
    public required string SpaceComplexity { get; set; }
    public string? VideoUrl { get; set; }
}
