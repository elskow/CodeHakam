namespace ContentService.DTOs.Requests;

public class UpdateDiscussionRequest
{
    public required string Title { get; set; }
    public required string Content { get; set; }
}
