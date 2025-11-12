namespace ContentService.DTOs.Requests;

public class UpdateProblemRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? InputFormat { get; set; }
    public string? OutputFormat { get; set; }
    public string? Constraints { get; set; }
    public string? Difficulty { get; set; }
    public int? TimeLimit { get; set; }
    public int? MemoryLimit { get; set; }
    public List<string>? Tags { get; set; }
    public string? Visibility { get; set; }
    public string? HintText { get; set; }
}
