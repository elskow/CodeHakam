using ContentService.Data;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Repositories.Impl;

public class TestCaseRepository : ITestCaseRepository
{
    private readonly ContentDbContext _context;

    public TestCaseRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<TestCase?> GetByIdAsync(long id)
    {
        return await _context.TestCases
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == id);
    }

    public async Task<IEnumerable<TestCase>> GetByProblemIdAsync(long problemId)
    {
        return await _context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId && tc.IsActive)
            .OrderBy(tc => tc.TestNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestCase>> GetSampleTestCasesAsync(long problemId)
    {
        return await _context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId && tc.IsSample && tc.IsActive)
            .OrderBy(tc => tc.TestNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestCase>> GetHiddenTestCasesAsync(long problemId)
    {
        return await _context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId && !tc.IsSample && tc.IsActive)
            .OrderBy(tc => tc.TestNumber)
            .ToListAsync();
    }

    public async Task<TestCase> CreateAsync(TestCase testCase)
    {
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task<TestCase> UpdateAsync(TestCase testCase)
    {
        _context.TestCases.Update(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var testCase = await _context.TestCases.FindAsync(id);
        if (testCase == null)
        {
            return false;
        }

        testCase.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountByProblemAsync(long problemId)
    {
        return await _context.TestCases
            .AsNoTracking()
            .CountAsync(tc => tc.ProblemId == problemId && tc.IsActive);
    }

    public async Task<int> GetNextTestNumberAsync(long problemId)
    {
        var maxTestNumber = await _context.TestCases
            .AsNoTracking()
            .Where(tc => tc.ProblemId == problemId)
            .MaxAsync(tc => (int?)tc.TestNumber);

        return (maxTestNumber ?? 0) + 1;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.TestCases
            .AsNoTracking()
            .AnyAsync(tc => tc.Id == id && tc.IsActive);
    }
}
