namespace ContentService.DTOs.Requests;

public class UploadTestCaseRequest
{
    public required IFormFile InputFile { get; set; }
    public required IFormFile OutputFile { get; set; }
    public bool IsSample { get; set; }
    public int TestNumber { get; set; }
}
