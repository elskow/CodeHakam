using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class EditorialRepository(ContentDbContext context) : IEditorialRepository
{
    public async Task<Editorial?> GetByIdAsync(long id)
    {
        return await context.Editorials
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Editorial?> GetByProblemIdAsync(long problemId)
    {
        return await context.Editorials
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProblemId == problemId);
    }

    public async Task<IEnumerable<Editorial>> GetPublishedEditorialsAsync(int page, int pageSize)
    {
        return await context.Editorials
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Editorial> CreateAsync(Editorial editorial)
    {
        await context.Editorials.AddAsync(editorial);
        await context.SaveChangesAsync();
        return editorial;
    }

    public async Task<Editorial> UpdateAsync(Editorial editorial)
    {
        editorial.UpdatedAt = DateTime.UtcNow;
        context.Editorials.Update(editorial);
        await context.SaveChangesAsync();
        return editorial;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var editorial = await context.Editorials.FindAsync(id);
        if (editorial == null)
        {
            return false;
        }

        context.Editorials.Remove(editorial);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PublishAsync(long id)
    {
        var editorial = await context.Editorials.FindAsync(id);
        if (editorial == null)
        {
            return false;
        }

        editorial.IsPublished = true;
        editorial.PublishedAt = DateTime.UtcNow;
        editorial.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnpublishAsync(long id)
    {
        var editorial = await context.Editorials.FindAsync(id);
        if (editorial == null)
        {
            return false;
        }

        editorial.IsPublished = false;
        editorial.PublishedAt = null;
        editorial.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsForProblemAsync(long problemId)
    {
        return await context.Editorials
            .AsNoTracking()
            .AnyAsync(e => e.ProblemId == problemId);
    }

    public async Task<int> GetPublishedCountAsync()
    {
        return await context.Editorials
            .AsNoTracking()
            .CountAsync(e => e.IsPublished);
    }
}
