namespace ContentService.Services.Implementations;

using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Logging;

public class DiscussionService : IDiscussionService
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<DiscussionService> _logger;

    public DiscussionService(
        IDiscussionRepository discussionRepository,
        IProblemRepository problemRepository,
        IEventPublisher eventPublisher,
        ILogger<DiscussionService> logger)
    {
        _discussionRepository = discussionRepository;
        _problemRepository = problemRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Discussion?> GetDiscussionAsync(long id, bool includeComments = false, CancellationToken cancellationToken = default)
    {
        return await _discussionRepository.GetByIdAsync(id, includeComments);
    }

    public async Task<(IEnumerable<Discussion> Discussions, int TotalCount)> GetDiscussionsAsync(
        long? problemId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (problemId.HasValue)
        {
            var discussions = await _discussionRepository.GetByProblemIdAsync(problemId.Value, page, pageSize);
            var count = await _discussionRepository.GetCountByProblemAsync(problemId.Value);
            return (discussions, count);
        }

        var allDiscussions = await _discussionRepository.GetAllAsync(page, pageSize);
        var totalCount = await _discussionRepository.GetTotalCountAsync();
        return (allDiscussions, totalCount);
    }

    public async Task<Discussion> CreateDiscussionAsync(
        long problemId,
        string title,
        string content,
        long authorId,
        CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        var discussion = new Discussion
        {
            ProblemId = problemId,
            Title = title,
            Content = content,
            UserId = authorId,
            VoteCount = 0,
            CommentCount = 0,
            IsLocked = false,
            IsPinned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _discussionRepository.CreateAsync(discussion);

        _logger.LogInformation("Discussion created: {DiscussionId} for problem {ProblemId} by user {AuthorId}",
            discussion.Id, problemId, authorId);

        await _eventPublisher.PublishDiscussionCreatedAsync(
            discussion.Id,
            problemId,
            title,
            authorId,
            cancellationToken);

        return discussion;
    }

    public async Task<Discussion> UpdateDiscussionAsync(
        long id,
        string title,
        string content,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var discussion = await _discussionRepository.GetByIdAsync(id, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {id} not found");
        }

        if (discussion.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the discussion author can update it");
        }

        discussion.Title = title;
        discussion.Content = content;
        discussion.UpdatedAt = DateTime.UtcNow;

        await _discussionRepository.UpdateAsync(discussion);

        _logger.LogInformation("Discussion updated: {DiscussionId} by user {UserId}", id, userId);

        return discussion;
    }

    public async Task DeleteDiscussionAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var discussion = await _discussionRepository.GetByIdAsync(id, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {id} not found");
        }

        if (discussion.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the discussion author can delete it");
        }

        await _discussionRepository.DeleteAsync(id);

        _logger.LogInformation("Discussion deleted: {DiscussionId} by user {UserId}", id, userId);
    }

    public async Task<DiscussionComment> AddCommentAsync(
        long discussionId,
        string content,
        long authorId,
        long? parentCommentId = null,
        CancellationToken cancellationToken = default)
    {
        var discussion = await _discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {discussionId} not found");
        }

        if (parentCommentId.HasValue)
        {
            var parentComment = await _discussionRepository.GetCommentByIdAsync(parentCommentId.Value);
            if (parentComment == null)
            {
                throw new InvalidOperationException($"Parent comment with ID {parentCommentId} not found");
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
            UserId = authorId,
            ParentId = parentCommentId,
            VoteCount = 0,
            IsAccepted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _discussionRepository.CreateCommentAsync(comment);
        await _discussionRepository.IncrementCommentCountAsync(discussionId);

        _logger.LogInformation("Comment added to discussion {DiscussionId} by user {AuthorId}",
            discussionId, authorId);

        return comment;
    }

    public async Task<DiscussionComment> UpdateCommentAsync(
        long commentId,
        string content,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _discussionRepository.GetCommentByIdAsync(commentId);
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

        await _discussionRepository.UpdateCommentAsync(comment);

        _logger.LogInformation("Comment updated: {CommentId} by user {UserId}", commentId, userId);

        return comment;
    }

    public async Task DeleteCommentAsync(long commentId, long userId, CancellationToken cancellationToken = default)
    {
        var comment = await _discussionRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
        {
            throw new InvalidOperationException($"Comment with ID {commentId} not found");
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("Only the comment author can delete it");
        }

        await _discussionRepository.DeleteCommentAsync(commentId);
        await _discussionRepository.DecrementCommentCountAsync(comment.DiscussionId);

        _logger.LogInformation("Comment deleted: {CommentId} by user {UserId}", commentId, userId);
    }

    public async Task VoteDiscussionAsync(long discussionId, bool upvote, long userId, CancellationToken cancellationToken = default)
    {
        var discussion = await _discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        if (discussion == null)
        {
            throw new InvalidOperationException($"Discussion with ID {discussionId} not found");
        }

        var increment = upvote ? 1 : -1;
        await _discussionRepository.IncrementVoteCountAsync(discussionId, increment);

        _logger.LogInformation("Discussion {DiscussionId} voted by user {UserId}: {VoteType}",
            discussionId, userId, upvote ? "upvote" : "downvote");
    }

    public async Task<bool> DiscussionExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _discussionRepository.ExistsAsync(id);
    }

    public async Task<bool> IsDiscussionAuthorAsync(long discussionId, long userId, CancellationToken cancellationToken = default)
    {
        var discussion = await _discussionRepository.GetByIdAsync(discussionId, includeComments: false);
        return discussion?.UserId == userId;
    }

    public async Task<bool> IsCommentAuthorAsync(long commentId, long userId, CancellationToken cancellationToken = default)
    {
        var comment = await _discussionRepository.GetCommentByIdAsync(commentId);
        return comment?.UserId == userId;
    }
}
