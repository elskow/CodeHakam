namespace ContentService.DTOs.Responses;

public class TestCaseCountResponse
{
    public long ProblemId { get; set; }
    public int TotalCount { get; set; }
    public int SampleCount { get; set; }
    public int HiddenCount { get; set; }
}