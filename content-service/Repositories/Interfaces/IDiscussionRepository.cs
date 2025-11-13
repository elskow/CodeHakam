using ContentService.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace ContentService.Repositories.Interfaces;

public interface IDiscussionRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<Discussion?> GetByIdAsync(long id, bool includeComments = false);
    Task<IEnumerable<Discussion>> GetByProblemIdAsync(long problemId, int page, int pageSize);
    Task<IEnumerable<Discussion>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> GetCountByProblemAsync(long problemId);
    Task<Discussion> CreateAsync(Discussion discussion);
    Task<Discussion> UpdateAsync(Discussion discussion);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
    Task IncrementVoteCountAsync(long discussionId, int increment);
    Task<bool> HasUserVotedAsync(long discussionId, long userId);
    Task<bool?> GetUserVoteAsync(long discussionId, long userId);
    Task RecordVoteAsync(long discussionId, long userId, bool isUpvote);
    Task UpdateVoteAsync(long discussionId, long userId, bool isUpvote);
    Task IncrementCommentCountAsync(long discussionId);
    Task DecrementCommentCountAsync(long discussionId);
    Task<bool> LockAsync(long id);
    Task<bool> UnlockAsync(long id);
    Task<bool> PinAsync(long id);
    Task<bool> UnpinAsync(long id);
    Task<DiscussionComment?> GetCommentByIdAsync(long commentId);
    Task<IEnumerable<DiscussionComment>> GetCommentsByDiscussionIdAsync(long discussionId);
    Task<DiscussionComment> CreateCommentAsync(DiscussionComment comment);
    Task<DiscussionComment> UpdateCommentAsync(DiscussionComment comment);
    Task<bool> DeleteCommentAsync(long commentId);
    Task IncrementCommentVoteCountAsync(long commentId, int increment);
}
