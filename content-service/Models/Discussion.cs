namespace ContentService.Models;

public sealed class Discussion
{
    public long Id { get; set; }
    public long? ProblemId { get; set; }
    public long UserId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public int VoteCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPinned { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Problem? Problem { get; set; }
    public ICollection<DiscussionComment> Comments { get; set; } = new List<DiscussionComment>();
}
