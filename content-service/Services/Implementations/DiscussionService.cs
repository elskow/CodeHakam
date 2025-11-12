using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;

namespace ContentService.Services.Implementations;

public class DiscussionService(
    IDiscussionRepository discussionRepository,
    IProblemRepository problemRepository,
    IEventPublisher eventPublisher,
    ILogger<DiscussionService> logger)
    : IDiscussionService
{
    public async Task<Discussion?> GetDiscussionAsync(long id, bool includeComments = false, CancellationToken cancellationToken = default)
    {
        return await discussionRepository.GetByIdAsync(id, includeComments);
    }

    public async Task<IEnumerable<Discussion>> GetDiscussionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await discussionRepository.GetAllAsync(page, pageSize);
    }

    public async Task<IEnumerable<Discussion>> GetDiscussionsByProblemAsync(
        long problemId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await discussionRepository.GetByProblemIdAsync(problemId, page, pageSize);
    }

    public async Task<int> GetTotalDiscussionsCountAsync(CancellationToken cancellationToken = default)
    {
        return await discussionRepository.GetTotalCountAsync();
    }

    public async Task<int> GetProblemDiscussionsCountAsync(long problemId, CancellationToken cancellationToken = default)
    {
        return await discussionRepository.GetCountByProblemAsync(problemId);
    }

    public async Task<Discussion> CreateDiscussionAsync(
        long problemId,
        long userId,
        string title,
        string content,
        CancellationToken cancellationToken = default)
    {
        var problem = await problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        var discussion = new Discussion
        {
            ProblemId = problemId,
            Title = title,
            Content = content,
            UserId = userId,
            VoteCount = 0,
            CommentCount = 0,
            IsLocked = false,
            IsPinned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await discussionRepository.CreateAsync(discussion);

        logger.LogInformation("Discussion created: {DiscussionId} for problem {ProblemId} by user {UserId}",
            discussion.Id, problemId, userId);

        await eventPublisher.PublishDiscussionCreatedAsync(
            discussion.Id,
            problemId,
            title,
            userId,
            cancellationToken);

        return discussion;
    }

    public async Task<Discussion> UpdateDiscussionAsync(
        long discussionId,
        long userId,
        string title,
        string content,
        CancellationToken cancellationToken = default)
    {
        var discussion = await discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {discussionId} not found");
        }

        if (discussion.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the discussion author can update it");
        }

        discussion.Title = title;
        discussion.Content = content;
        discussion.UpdatedAt = DateTime.UtcNow;

        await discussionRepository.UpdateAsync(discussion);

        logger.LogInformation("Discussion updated: {DiscussionId} by user {UserId}", discussionId, userId);

        return discussion;
    }

    public async Task DeleteDiscussionAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var discussion = await discussionRepository.GetByIdAsync(id, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {id} not found");
        }

        if (discussion.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the discussion author can delete it");
        }

        await discussionRepository.DeleteAsync(id);

        logger.LogInformation("Discussion deleted: {DiscussionId} by user {UserId}", id, userId);
    }

    public async Task<DiscussionComment> AddCommentAsync(
        long discussionId,
        long userId,
        string content,
        long? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var discussion = await discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {discussionId} not found");
        }

        if (parentId.HasValue)
        {
            var parentComment = await discussionRepository.GetCommentByIdAsync(parentId.Value);
            if (parentComment == null)
            {
                throw new InvalidOperationException($"Parent comment with ID {parentId.Value} not found");
            }

            if (parentComment.DiscussionId != discussionId)
            {
                throw new InvalidOperationException("Parent comment does not belong to this discussion");
            }
        }

        var comment = new DiscussionComment
        {
            DiscussionId = discussionId,
            Content = content,
            UserId = userId,
            ParentId = parentId,
            VoteCount = 0,
            IsAccepted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await discussionRepository.CreateCommentAsync(comment);
        await discussionRepository.IncrementCommentCountAsync(discussionId);

        logger.LogInformation("Comment added to discussion {DiscussionId} by user {UserId}",
            discussionId, userId);

        return comment;
    }

    public async Task<DiscussionComment?> GetCommentAsync(long commentId, CancellationToken cancellationToken = default)
    {
        return await discussionRepository.GetCommentByIdAsync(commentId);
    }

    public async Task<DiscussionComment> UpdateCommentAsync(
        long commentId,
        string content,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await discussionRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
        {
            throw new InvalidOperationException($"Comment with ID {commentId} not found");
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the comment author can update it");
        }

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;

        await discussionRepository.UpdateCommentAsync(comment);

        logger.LogInformation("Comment updated: {CommentId} by user {UserId}", commentId, userId);

        return comment;
    }

    public async Task DeleteCommentAsync(long commentId, long userId, CancellationToken cancellationToken = default)
    {
        var comment = await discussionRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
        {
            throw new InvalidOperationException($"Comment with ID {commentId} not found");
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the comment author can delete it");
        }

        await discussionRepository.DeleteCommentAsync(commentId);
        await discussionRepository.DecrementCommentCountAsync(comment.DiscussionId);

        logger.LogInformation("Comment deleted: {CommentId} by user {UserId}", commentId, userId);
    }

    public async Task VoteDiscussionAsync(long discussionId, bool upvote, long userId, CancellationToken cancellationToken = default)
    {
        var discussion = await discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {discussionId} not found");
        }

        var increment = upvote ? 1 : -1;
        await discussionRepository.IncrementVoteCountAsync(discussionId, increment);

        logger.LogInformation("Discussion {DiscussionId} voted by user {UserId}: {VoteType}",
            discussionId, userId, upvote ? "upvote" : "downvote");
    }

    public async Task<bool> DiscussionExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await discussionRepository.ExistsAsync(id);
    }

    public async Task<bool> IsDiscussionAuthorAsync(long discussionId, long userId, CancellationToken cancellationToken = default)
    {
        var discussion = await discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        return discussion?.UserId == userId;
    }

    public async Task<bool> IsCommentAuthorAsync(long commentId, long userId, CancellationToken cancellationToken = default)
    {
        var comment = await discussionRepository.GetCommentByIdAsync(commentId);
        return comment?.UserId == userId;
    }

    public async Task<(IEnumerable<Discussion> Discussions, int TotalCount)> GetDiscussionsAsync(
        long? problemId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (problemId.HasValue)
        {
            var discussions = await discussionRepository.GetByProblemIdAsync(problemId.Value, page, pageSize);
            var count = await discussionRepository.GetCountByProblemAsync(problemId.Value);
            return (discussions, count);
        }

        var allDiscussions = await discussionRepository.GetAllAsync(page, pageSize);
        var totalCount = await discussionRepository.GetTotalCountAsync();
        return (allDiscussions, totalCount);
    }
}
