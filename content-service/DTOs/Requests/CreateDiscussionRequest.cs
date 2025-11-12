namespace ContentService.DTOs.Requests;

public class CreateDiscussionRequest
{
    public required long ProblemId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
}
