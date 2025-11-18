using ContentService.Enums;

namespace ContentService.DTOs.Requests;

public class CreateProblemRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string InputFormat { get; set; }
    public required string OutputFormat { get; set; }
    public required string Constraints { get; set; }
    public required Difficulty Difficulty { get; set; }
    public int TimeLimit { get; set; } = 2000;
    public int MemoryLimit { get; set; } = 262144;
    public List<string> Tags { get; set; } = new();
    public ProblemVisibility? Visibility { get; set; }
    public string? HintText { get; set; }
}