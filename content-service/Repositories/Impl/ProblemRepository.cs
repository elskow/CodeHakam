using ContentService.Data;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class ProblemRepository(ContentDbContext context) : IProblemRepository
{
    public async Task<Problem?> GetByIdAsync(long id, bool includeRelated = false)
    {
        var query = context.Problems.AsQueryable();

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
        var query = context.Problems.AsQueryable();

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
        return await context.Problems
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
        var query = context.Problems
            .AsNoTracking()
            .Where(p => p.IsActive);

        query = visibility.HasValue ? query.Where(p => p.Visibility == visibility.Value) : query.Where(p => p.Visibility == ProblemVisibility.Public);

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
        return await context.Problems
            .AsNoTracking()
            .CountAsync(p => p.IsActive && p.Visibility == ProblemVisibility.Public);
    }

    public async Task<int> GetSearchCountAsync(
        string? searchTerm,
        Difficulty? difficulty,
        string? tag,
        ProblemVisibility? visibility)
    {
        var query = context.Problems
            .AsNoTracking()
            .Where(p => p.IsActive);

        query = visibility.HasValue ? query.Where(p => p.Visibility == visibility.Value) : query.Where(p => p.Visibility == ProblemVisibility.Public);

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
        await context.Problems.AddAsync(problem);
        await context.SaveChangesAsync();
        return problem;
    }

    public async Task<Problem> UpdateAsync(Problem problem)
    {
        problem.UpdatedAt = DateTime.UtcNow;
        context.Problems.Update(problem);
        await context.SaveChangesAsync();
        return problem;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var problem = await context.Problems.FindAsync(id);
        if (problem == null)
        {
            return false;
        }

        problem.IsActive = false;
        problem.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await context.Problems
            .AsNoTracking()
            .AnyAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        return await context.Problems
            .AsNoTracking()
            .AnyAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<Problem>> GetByAuthorAsync(long authorId, int page, int pageSize)
    {
        return await context.Problems
            .AsNoTracking()
            .Where(p => p.AuthorId == authorId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task IncrementViewCountAsync(long id)
    {
        var problem = await context.Problems.FindAsync(id);
        if (problem != null)
        {
            problem.ViewCount++;
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateStatisticsAsync(long id, int submissionCount, int acceptedCount)
    {
        var problem = await context.Problems.FindAsync(id);
        if (problem != null)
        {
            problem.SubmissionCount = submissionCount;
            problem.AcceptedCount = acceptedCount;
            problem.AcceptanceRate = submissionCount > 0
                ? Math.Round((decimal)acceptedCount / submissionCount * 100, decimals: 2)
                : 0;
            problem.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<string>> GetAllTagsAsync()
    {
        return await context.ProblemTags
            .AsNoTracking()
            .Select(pt => pt.Tag)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }
}
