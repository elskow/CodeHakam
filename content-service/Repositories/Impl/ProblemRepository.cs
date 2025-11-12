using ContentService.Data;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class ProblemRepository : IProblemRepository
{
    private readonly ContentDbContext _context;

    public ProblemRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<Problem?> GetByIdAsync(long id, bool includeRelated = false)
    {
        var query = _context.Problems.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(p => p.TestCases)
                .Include(p => p.Editorial)
                .Include(p => p.Tags);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Problem?> GetBySlugAsync(string slug, bool includeRelated = false)
    {
        var query = _context.Problems.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(p => p.TestCases)
                .Include(p => p.Editorial)
                .Include(p => p.Tags);
        }

        return await query.FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<Problem>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Problems
            .AsNoTracking()
            .Where(p => p.IsActive && p.Visibility == ProblemVisibility.Public)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Problem>> SearchAsync(
        string? searchTerm,
        Difficulty? difficulty,
        string? tag,
        ProblemVisibility? visibility,
        int page,
        int pageSize)
    {
        var query = _context.Problems
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (visibility.HasValue)
        {
            query = query.Where(p => p.Visibility == visibility.Value);
        }
        else
        {
            query = query.Where(p => p.Visibility == ProblemVisibility.Public);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(lowerSearchTerm) ||
                p.Description.ToLower().Contains(lowerSearchTerm));
        }

        if (difficulty.HasValue)
        {
            query = query.Where(p => p.Difficulty == difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(p => p.Tags.Any(t => t.Tag == tag.ToLower()));
        }

        return await query
            .Include(p => p.Tags)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Problems
            .AsNoTracking()
            .CountAsync(p => p.IsActive && p.Visibility == ProblemVisibility.Public);
    }

    public async Task<int> GetSearchCountAsync(
        string? searchTerm,
        Difficulty? difficulty,
        string? tag,
        ProblemVisibility? visibility)
    {
        var query = _context.Problems
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (visibility.HasValue)
        {
            query = query.Where(p => p.Visibility == visibility.Value);
        }
        else
        {
            query = query.Where(p => p.Visibility == ProblemVisibility.Public);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(lowerSearchTerm) ||
                p.Description.ToLower().Contains(lowerSearchTerm));
        }

        if (difficulty.HasValue)
        {
            query = query.Where(p => p.Difficulty == difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(p => p.Tags.Any(t => t.Tag == tag.ToLower()));
        }

        return await query.CountAsync();
    }

    public async Task<Problem> CreateAsync(Problem problem)
    {
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();
        return problem;
    }

    public async Task<Problem> UpdateAsync(Problem problem)
    {
        problem.UpdatedAt = DateTime.UtcNow;
        _context.Problems.Update(problem);
        await _context.SaveChangesAsync();
        return problem;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var problem = await _context.Problems.FindAsync(id);
        if (problem == null)
        {
            return false;
        }

        problem.IsActive = false;
        problem.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.Problems
            .AsNoTracking()
            .AnyAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        return await _context.Problems
            .AsNoTracking()
            .AnyAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<Problem>> GetByAuthorAsync(long authorId, int page, int pageSize)
    {
        return await _context.Problems
            .AsNoTracking()
            .Where(p => p.AuthorId == authorId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task IncrementViewCountAsync(long id)
    {
        var problem = await _context.Problems.FindAsync(id);
        if (problem != null)
        {
            problem.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateStatisticsAsync(long id, int submissionCount, int acceptedCount)
    {
        var problem = await _context.Problems.FindAsync(id);
        if (problem != null)
        {
            problem.SubmissionCount = submissionCount;
            problem.AcceptedCount = acceptedCount;
            problem.AcceptanceRate = submissionCount > 0
                ? Math.Round((decimal)acceptedCount / submissionCount * 100, 2)
                : 0;
            problem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<string>> GetAllTagsAsync()
    {
        return await _context.ProblemTags
            .AsNoTracking()
            .Select(pt => pt.Tag)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }
}
