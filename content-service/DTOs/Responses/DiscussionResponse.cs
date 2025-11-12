namespace ContentService.DTOs.Responses;

public class DiscussionResponse
{
    public long Id { get; set; }
    public long? ProblemId { get; set; }
    public long UserId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public int VoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CommentResponse> Comments { get; set; } = new();
}

public class CommentResponse
{
    public long Id { get; set; }
    public long DiscussionId { get; set; }
    public long UserId { get; set; }
    public long? ParentId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
