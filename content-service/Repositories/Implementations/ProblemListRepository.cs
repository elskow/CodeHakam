using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Implementations;

public sealed class ProblemListRepository(ContentDbContext context) : IProblemListRepository
{
    public async Task<ProblemList?> GetByIdAsync(long id)
    {
        return await context.ProblemLists
            .AsNoTracking()
            .FirstOrDefaultAsync(pl => pl.Id == id);
    }

    public async Task<IEnumerable<ProblemList>> GetByOwnerAsync(long ownerId, int page, int pageSize)
    {
        return await context.ProblemLists
            .AsNoTracking()
            .Where(pl => pl.OwnerId == ownerId)
            .OrderByDescending(pl => pl.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProblemList>> GetPublicListsAsync(int page, int pageSize)
    {
        return await context.ProblemLists
            .AsNoTracking()
            .Where(pl => pl.IsPublic)
            .OrderByDescending(pl => pl.ViewCount)
            .ThenByDescending(pl => pl.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalPublicCountAsync()
    {
        return await context.ProblemLists
            .AsNoTracking()
            .CountAsync(pl => pl.IsPublic);
    }

    public async Task<int> GetCountByOwnerAsync(long ownerId)
    {
        return await context.ProblemLists
            .AsNoTracking()
            .CountAsync(pl => pl.OwnerId == ownerId);
    }

    public async Task<ProblemList> CreateAsync(ProblemList list)
    {
        await context.ProblemLists.AddAsync(list);
        await context.SaveChangesAsync();
        return list;
    }

    public async Task<ProblemList> UpdateAsync(ProblemList list)
    {
        list.UpdatedAt = DateTime.UtcNow;
        context.ProblemLists.Update(list);
        await context.SaveChangesAsync();
        return list;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var list = await context.ProblemLists.FindAsync(id);
        if (list == null)
        {
            return false;
        }

        context.ProblemLists.Remove(list);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await context.ProblemLists
            .AsNoTracking()
            .AnyAsync(pl => pl.Id == id);
    }

    public async Task IncrementViewCountAsync(long id)
    {
        var list = await context.ProblemLists.FindAsync(id);
        if (list != null)
        {
            list.ViewCount++;
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> AddProblemAsync(long listId, long problemId)
    {
        var list = await context.ProblemLists.FindAsync(listId);
        if (list == null)
        {
            return false;
        }

        if (list.ProblemIds.Contains(problemId))
        {
            return false;
        }

        var newProblemIds = list.ProblemIds.Append(problemId).ToArray();
        list.ProblemIds = newProblemIds;
        list.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveProblemAsync(long listId, long problemId)
    {
        var list = await context.ProblemLists.FindAsync(listId);
        if (list == null)
        {
            return false;
        }

        if (!list.ProblemIds.Contains(problemId))
        {
            return false;
        }

        var newProblemIds = list.ProblemIds.Where(id => id != problemId).ToArray();
        list.ProblemIds = newProblemIds;
        list.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ContainsProblemAsync(long listId, long problemId)
    {
        var list = await context.ProblemLists
            .AsNoTracking()
            .FirstOrDefaultAsync(pl => pl.Id == listId);

        return list?.ProblemIds.Contains(problemId) ?? false;
    }
}
