using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class DiscussionRepository(ContentDbContext context) : IDiscussionRepository
{
    public async Task<Discussion?> GetByIdAsync(long id, bool includeComments = false)
    {
        var query = context.Discussions.AsNoTracking();

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
            .Where(d => d.ProblemId == problemId)
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
            .CountAsync();
    }

    public async Task<int> GetCountByProblemAsync(long problemId)
    {
        return await context.Discussions
            .AsNoTracking()
            .CountAsync(d => d.ProblemId == problemId);
    }

    public async Task<Discussion> CreateAsync(Discussion discussion)
    {
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
            .AnyAsync(d => d.Id == id);
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
        await context.SaveChangesAsync();
        await IncrementCommentCountAsync(comment.DiscussionId);
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

        var discussionId = comment.DiscussionId;
        context.DiscussionComments.Remove(comment);
        await context.SaveChangesAsync();
        await DecrementCommentCountAsync(discussionId);
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
