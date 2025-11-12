namespace ContentService.Models;

public sealed class DiscussionComment
{
    public long Id { get; set; }
    public long DiscussionId { get; set; }
    public long? ParentId { get; set; }
    public long UserId { get; set; }
    public required string Content { get; set; }
    public int VoteCount { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Discussion Discussion { get; set; } = null!;
    public DiscussionComment? Parent { get; set; }
    public ICollection<DiscussionComment> Replies { get; set; } = new List<DiscussionComment>();
}
