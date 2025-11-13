namespace ContentService.DTOs.Requests;

public class AddCommentRequest
{
    public required string Content { get; set; }
    public long? ParentId { get; set; }
}
