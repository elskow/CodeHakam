namespace ContentService.DTOs.Requests;

public class AddCommentRequest
{
    public required long DiscussionId { get; set; }
    public required string Content { get; set; }
    public long? ParentId { get; set; }
}
