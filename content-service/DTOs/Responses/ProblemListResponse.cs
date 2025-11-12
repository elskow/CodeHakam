namespace ContentService.DTOs.Responses;

public class ProblemListResponse
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public long OwnerId { get; set; }
    public bool IsPublic { get; set; }
    public int ProblemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProblemResponse> Problems { get; set; } = new();
}
