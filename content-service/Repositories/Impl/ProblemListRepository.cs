using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class ProblemListRepository : IProblemListRepository
{
    private readonly ContentDbContext _context;

    public ProblemListRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<ProblemList?> GetByIdAsync(long id)
    {
        return await _context.ProblemLists
            .AsNoTracking()
            .FirstOrDefaultAsync(pl => pl.Id == id);
    }

    public async Task<IEnumerable<ProblemList>> GetByOwnerAsync(long ownerId, int page, int pageSize)
    {
        return await _context.ProblemLists
            .AsNoTracking()
            .Where(pl => pl.OwnerId == ownerId)
            .OrderByDescending(pl => pl.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProblemList>> GetPublicListsAsync(int page, int pageSize)
    {
        return await _context.ProblemLists
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
        return await _context.ProblemLists
            .AsNoTracking()
            .CountAsync(pl => pl.IsPublic);
    }

    public async Task<int> GetCountByOwnerAsync(long ownerId)
    {
        return await _context.ProblemLists
            .AsNoTracking()
            .CountAsync(pl => pl.OwnerId == ownerId);
    }

    public async Task<ProblemList> CreateAsync(ProblemList list)
    {
        await _context.ProblemLists.AddAsync(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<ProblemList> UpdateAsync(ProblemList list)
    {
        list.UpdatedAt = DateTime.UtcNow;
        _context.ProblemLists.Update(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var list = await _context.ProblemLists.FindAsync(id);
        if (list == null)
        {
            return false;
        }

        _context.ProblemLists.Remove(list);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.ProblemLists
            .AsNoTracking()
            .AnyAsync(pl => pl.Id == id);
    }

    public async Task IncrementViewCountAsync(long id)
    {
        var list = await _context.ProblemLists.FindAsync(id);
        if (list != null)
        {
            list.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> AddProblemAsync(long listId, long problemId)
    {
        var list = await _context.ProblemLists.FindAsync(listId);
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
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveProblemAsync(long listId, long problemId)
    {
        var list = await _context.ProblemLists.FindAsync(listId);
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
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ContainsProblemAsync(long listId, long problemId)
    {
        var list = await _context.ProblemLists
            .AsNoTracking()
            .FirstOrDefaultAsync(pl => pl.Id == listId);

        return list?.ProblemIds.Contains(problemId) ?? false;
    }
}
