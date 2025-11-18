using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ContentService.Repositories.Implementations;

public sealed class DiscussionRepository(ContentDbContext context) : IDiscussionRepository
{
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync();
    }

    public async Task<Discussion?> GetByIdAsync(long id, bool includeComments = false)
    {
        var query = context.Discussions.AsNoTracking().Where(d => d.IsActive);

        if (includeComments)
        {
            query = query.Include(d => d.Comments.OrderBy(c => c.CreatedAt));
        }

        return await query.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Discussion>> GetByProblemIdAsync(long problemId, int page, int pageSize)
    {
        return await context.Discussions
            .AsNoTracking()
            .Where(d => d.ProblemId == problemId && d.IsActive)
            .OrderByDescending(d => d.IsPinned)
            .ThenByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Discussion>> GetAllAsync(int page, int pageSize)
    {
        return await context.Discussions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.IsPinned)
            .ThenByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await context.Discussions
            .AsNoTracking()
            .CountAsync(d => d.IsActive);
    }

    public async Task<int> GetCountByProblemAsync(long problemId)
    {
        return await context.Discussions
            .AsNoTracking()
            .CountAsync(d => d.ProblemId == problemId && d.IsActive);
    }

    public async Task<Discussion> CreateAsync(Discussion discussion)
    {
        discussion.IsActive = true;
        await context.Discussions.AddAsync(discussion);
        await context.SaveChangesAsync();
        return discussion;
    }

    public async Task<Discussion> UpdateAsync(Discussion discussion)
    {
        discussion.UpdatedAt = DateTime.UtcNow;
        context.Discussions.Update(discussion);
        await context.SaveChangesAsync();
        return discussion;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var discussion = await context.Discussions.FindAsync(id);
        if (discussion == null)
        {
            return false;
        }

        context.Discussions.Remove(discussion);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await context.Discussions
            .AsNoTracking()
            .AnyAsync(d => d.Id == id && d.IsActive);
    }

    public async Task<bool> HasUserVotedAsync(long discussionId, long userId)
    {
        return await context.DiscussionVotes
            .AsNoTracking()
            .AnyAsync(v => v.DiscussionId == discussionId && v.UserId == userId);
    }

    public async Task<bool?> GetUserVoteAsync(long discussionId, long userId)
    {
        var vote = await context.DiscussionVotes
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.DiscussionId == discussionId && v.UserId == userId);

        return vote?.IsUpvote;
    }

    public async Task RecordVoteAsync(long discussionId, long userId, bool isUpvote)
    {
        var vote = new DiscussionVote
        {
            DiscussionId = discussionId,
            UserId = userId,
            IsUpvote = isUpvote,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.DiscussionVotes.AddAsync(vote);
        await context.SaveChangesAsync();
    }

    public async Task UpdateVoteAsync(long discussionId, long userId, bool isUpvote)
    {
        var vote = await context.DiscussionVotes
            .FirstOrDefaultAsync(v => v.DiscussionId == discussionId && v.UserId == userId);

        if (vote != null)
        {
            vote.IsUpvote = isUpvote;
            vote.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementVoteCountAsync(long discussionId, int increment)
    {
        var discussion = await context.Discussions.FindAsync(discussionId);
        if (discussion != null)
        {
            discussion.VoteCount += increment;
            discussion.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementCommentCountAsync(long discussionId)
    {
        var discussion = await context.Discussions.FindAsync(discussionId);
        if (discussion != null)
        {
            discussion.CommentCount++;
            discussion.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task DecrementCommentCountAsync(long discussionId)
    {
        var discussion = await context.Discussions.FindAsync(discussionId);
        if (discussion is { CommentCount: > 0 })
        {
            discussion.CommentCount--;
            discussion.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> LockAsync(long id)
    {
        var discussion = await context.Discussions.FindAsync(id);
        if (discussion == null)
        {
            return false;
        }

        discussion.IsLocked = true;
        discussion.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlockAsync(long id)
    {
        var discussion = await context.Discussions.FindAsync(id);
        if (discussion == null)
        {
            return false;
        }

        discussion.IsLocked = false;
        discussion.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PinAsync(long id)
    {
        var discussion = await context.Discussions.FindAsync(id);
        if (discussion == null)
        {
            return false;
        }

        discussion.IsPinned = true;
        discussion.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnpinAsync(long id)
    {
        var discussion = await context.Discussions.FindAsync(id);
        if (discussion == null)
        {
            return false;
        }

        discussion.IsPinned = false;
        discussion.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<DiscussionComment?> GetCommentByIdAsync(long commentId)
    {
        return await context.DiscussionComments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commentId);
    }

    public async Task<IEnumerable<DiscussionComment>> GetCommentsByDiscussionIdAsync(long discussionId)
    {
        return await context.DiscussionComments
            .AsNoTracking()
            .Where(c => c.DiscussionId == discussionId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<DiscussionComment> CreateCommentAsync(DiscussionComment comment)
    {
        await context.DiscussionComments.AddAsync(comment);

        var discussion = await context.Discussions.FindAsync(comment.DiscussionId);
        if (discussion != null)
        {
            discussion.CommentCount++;
            discussion.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return comment;
    }

    public async Task<DiscussionComment> UpdateCommentAsync(DiscussionComment comment)
    {
        comment.UpdatedAt = DateTime.UtcNow;
        context.DiscussionComments.Update(comment);
        await context.SaveChangesAsync();
        return comment;
    }

    public async Task<bool> DeleteCommentAsync(long commentId)
    {
        var comment = await context.DiscussionComments.FindAsync(commentId);
        if (comment == null)
        {
            return false;
        }

        var discussion = await context.Discussions.FindAsync(comment.DiscussionId);
        if (discussion is { CommentCount: > 0 })
        {
            discussion.CommentCount--;
            discussion.UpdatedAt = DateTime.UtcNow;
        }

        context.DiscussionComments.Remove(comment);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task IncrementCommentVoteCountAsync(long commentId, int increment)
    {
        var comment = await context.DiscussionComments.FindAsync(commentId);
        if (comment != null)
        {
            comment.VoteCount += increment;
            comment.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}
