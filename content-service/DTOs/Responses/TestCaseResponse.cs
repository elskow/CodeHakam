namespace ContentService.DTOs.Responses;

public class TestCaseResponse
{
    public long Id { get; set; }
    public long ProblemId { get; set; }
    public int TestNumber { get; set; }
    public bool IsSample { get; set; }
    public string? InputFileUrl { get; set; }
    public string? OutputFileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
