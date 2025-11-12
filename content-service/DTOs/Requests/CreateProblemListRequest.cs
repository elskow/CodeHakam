namespace ContentService.DTOs.Requests;

public class CreateProblemListRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public List<long> ProblemIds { get; set; } = new();
}
