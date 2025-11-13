namespace ContentService.DTOs.Requests;

public class UpdateEditorialRequest
{
    public required string Content { get; set; }
    public required string TimeComplexity { get; set; }
    public required string SpaceComplexity { get; set; }
    public string? VideoUrl { get; set; }
}
