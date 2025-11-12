using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class TestCaseRepository(ContentDbContext context) : ITestCaseRepository
{
    public async Task<TestCase?> GetByIdAsync(long id)
    {
        return await context.TestCases
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == id);
    }

    public async Task<IEnumerable<TestCase>> GetByProblemIdAsync(long problemId)
    {
        return await context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId && tc.IsActive)
            .OrderBy(tc => tc.TestNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestCase>> GetSampleTestCasesAsync(long problemId)
    {
        return await context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId && tc.IsSample && tc.IsActive)
            .OrderBy(tc => tc.TestNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestCase>> GetHiddenTestCasesAsync(long problemId)
    {
        return await context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId && !tc.IsSample && tc.IsActive)
            .OrderBy(tc => tc.TestNumber)
            .ToListAsync();
    }

    public async Task<TestCase> CreateAsync(TestCase testCase)
    {
        await context.TestCases.AddAsync(testCase);
        await context.SaveChangesAsync();
        return testCase;
    }

    public async Task<TestCase> UpdateAsync(TestCase testCase)
    {
        context.TestCases.Update(testCase);
        await context.SaveChangesAsync();
        return testCase;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var testCase = await context.TestCases.FindAsync(id);
        if (testCase == null)
        {
            return false;
        }

        testCase.IsActive = false;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountByProblemAsync(long problemId)
    {
        return await context.TestCases
            .AsNoTracking()
            .CountAsync(tc => tc.ProblemId == problemId && tc.IsActive);
    }

    public async Task<int> GetNextTestNumberAsync(long problemId)
    {
        var maxTestNumber = await context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId)
            .MaxAsync(tc => (int?)tc.TestNumber);

        return (maxTestNumber ?? 0) + 1;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await context.TestCases
            .AsNoTracking()
            .AnyAsync(tc => tc.Id == id && tc.IsActive);
    }
}
