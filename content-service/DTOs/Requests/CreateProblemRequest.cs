namespace ContentService.DTOs.Requests;

public class CreateProblemRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string InputFormat { get; set; }
    public required string OutputFormat { get; set; }
    public required string Constraints { get; set; }
    public required string Difficulty { get; set; }
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Visibility { get; set; }
    public string? HintText { get; set; }
}
