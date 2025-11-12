using ContentService.Models;

namespace ContentService.Repositories.Interfaces;

public interface ITestCaseRepository
{
    Task<TestCase?> GetByIdAsync(long id);
    Task<IEnumerable<TestCase>> GetByProblemIdAsync(long problemId);
    Task<IEnumerable<TestCase>> GetSampleTestCasesAsync(long problemId);
    Task<IEnumerable<TestCase>> GetHiddenTestCasesAsync(long problemId);
    Task<TestCase> CreateAsync(TestCase testCase);
    Task<TestCase> UpdateAsync(TestCase testCase);
    Task<bool> DeleteAsync(long id);
    Task<int> GetCountByProblemAsync(long problemId);
    Task<int> GetNextTestNumberAsync(long problemId);
    Task<bool> ExistsAsync(long id);
}
