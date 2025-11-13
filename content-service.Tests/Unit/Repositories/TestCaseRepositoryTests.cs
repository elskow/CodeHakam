using ContentService.Data;
using ContentService.Repositories.Implementations;
using ContentService.Tests.Helpers;
using FluentAssertions;

namespace ContentService.Tests.Unit.Repositories;

public class TestCaseRepositoryTests : IDisposable
{
    private readonly ContentDbContext _context;
    private readonly TestCaseRepository _repository;

    public TestCaseRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"TestCaseRepositoryTests_{Guid.NewGuid()}");
        _repository = new TestCaseRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnTestCase()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id);
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(testCase.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(testCase.Id);
        result.ProblemId.Should().Be(problem.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProblemIdAsync_ShouldReturnAllActiveTestCasesForProblem()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase1 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 1);
        var testCase2 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 2);
        var inactiveTestCase = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 3, isActive: false);

        await _context.TestCases.AddRangeAsync(testCase1, testCase2, inactiveTestCase);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByProblemIdAsync(problem.Id);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(tc => tc.IsActive);
        result.Should().BeInAscendingOrder(tc => tc.TestNumber);
    }

    [Fact]
    public async Task GetByProblemIdAsync_WithNonExistingProblem_ShouldReturnEmpty()
    {
        var result = await _repository.GetByProblemIdAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSampleTestCasesAsync_ShouldReturnOnlySampleTestCases()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var sampleTestCase = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 1, isSample: true);
        var hiddenTestCase = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 2, isSample: false);

        await _context.TestCases.AddRangeAsync(sampleTestCase, hiddenTestCase);
        await _context.SaveChangesAsync();

        var result = await _repository.GetSampleTestCasesAsync(problem.Id);

        result.Should().HaveCount(1);
        result.First().IsSample.Should().BeTrue();
    }

    [Fact]
    public async Task GetHiddenTestCasesAsync_ShouldReturnOnlyHiddenTestCases()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var sampleTestCase = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 1, isSample: true);
        var hiddenTestCase1 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 2, isSample: false);
        var hiddenTestCase2 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 3, isSample: false);

        await _context.TestCases.AddRangeAsync(sampleTestCase, hiddenTestCase1, hiddenTestCase2);
        await _context.SaveChangesAsync();

        var result = await _repository.GetHiddenTestCasesAsync(problem.Id);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(tc => !tc.IsSample);
    }

    [Fact]
    public async Task CreateAsync_WithValidTestCase_ShouldCreateSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id);

        var result = await _repository.CreateAsync(testCase);

        result.Id.Should().BeGreaterThan(0);
        result.ProblemId.Should().Be(problem.Id);

        var savedTestCase = await _context.TestCases.FindAsync(result.Id);
        savedTestCase.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingTestCase_ShouldUpdateSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id);
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        testCase.IsSample = false;
        var result = await _repository.UpdateAsync(testCase);

        result.IsSample.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingTestCase_ShouldMarkAsInactive()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id);
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(testCase.Id);

        result.Should().BeTrue();

        var deletedTestCase = await _context.TestCases.FindAsync(testCase.Id);
        deletedTestCase!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingTestCase_ShouldReturnFalse()
    {
        var result = await _repository.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountByProblemAsync_ShouldReturnActiveTestCaseCount()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase1 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 1);
        var testCase2 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 2);
        var inactiveTestCase = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 3, isActive: false);

        await _context.TestCases.AddRangeAsync(testCase1, testCase2, inactiveTestCase);
        await _context.SaveChangesAsync();

        var count = await _repository.GetCountByProblemAsync(problem.Id);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetCountByProblemAsync_WithNonExistingProblem_ShouldReturnZero()
    {
        var count = await _repository.GetCountByProblemAsync(999);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetNextTestNumberAsync_WithExistingTestCases_ShouldReturnNextNumber()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase1 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 1);
        var testCase2 = TestDataBuilder.CreateTestCase(problemId: problem.Id, testNumber: 2);

        await _context.TestCases.AddRangeAsync(testCase1, testCase2);
        await _context.SaveChangesAsync();

        var nextNumber = await _repository.GetNextTestNumberAsync(problem.Id);

        nextNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetNextTestNumberAsync_WithNoTestCases_ShouldReturnOne()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var nextNumber = await _repository.GetNextTestNumberAsync(problem.Id);

        nextNumber.Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingActiveTestCase_ShouldReturnTrue()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id);
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(testCase.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithInactiveTestCase_ShouldReturnFalse()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id, isActive: false);
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(testCase.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingTestCase_ShouldReturnFalse()
    {
        var result = await _repository.ExistsAsync(999);

        result.Should().BeFalse();
    }
}
