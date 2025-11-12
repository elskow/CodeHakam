namespace ContentService.Services.Interfaces;

using ContentService.Models;

public interface IDiscussionService
{
    Task<Discussion?> GetDiscussionAsync(long id, bool includeComments = false, CancellationToken cancellationToken = default);

    Task<IEnumerable<Discussion>> GetDiscussionsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IEnumerable<Discussion>> GetDiscussionsByProblemAsync(long problemId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetTotalDiscussionsCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetProblemDiscussionsCountAsync(long problemId, CancellationToken cancellationToken = default);

    Task<Discussion> CreateDiscussionAsync(
        long problemId,
        long userId,
        string title,
        string content,
        CancellationToken cancellationToken = default);

    Task<Discussion> UpdateDiscussionAsync(
        long discussionId,
        long userId,
        string title,
        string content,
        CancellationToken cancellationToken = default);

    Task DeleteDiscussionAsync(long id, long userId, CancellationToken cancellationToken = default);

    Task<DiscussionComment> AddCommentAsync(
        long discussionId,
        long userId,
        string content,
        long? parentId = null,
        CancellationToken cancellationToken = default);

    Task<DiscussionComment?> GetCommentAsync(long commentId, CancellationToken cancellationToken = default);

    Task<DiscussionComment> UpdateCommentAsync(
        long commentId,
        string content,
        long userId,
        CancellationToken cancellationToken = default);

    Task DeleteCommentAsync(long commentId, long userId, CancellationToken cancellationToken = default);

    Task VoteDiscussionAsync(long discussionId, bool upvote, long userId, CancellationToken cancellationToken = default);

    Task<bool> DiscussionExistsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> IsDiscussionAuthorAsync(long discussionId, long userId, CancellationToken cancellationToken = default);

    Task<bool> IsCommentAuthorAsync(long commentId, long userId, CancellationToken cancellationToken = default);
}
