namespace ContentService.Models;

public sealed class DiscussionVote
{
    public long Id { get; set; }
    public long DiscussionId { get; set; }
    public long UserId { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Discussion Discussion { get; set; } = null!;
}
