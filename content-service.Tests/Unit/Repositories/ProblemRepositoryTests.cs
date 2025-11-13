using ContentService.Data;
using ContentService.Enums;
using ContentService.Repositories.Implementations;
using ContentService.Tests.Helpers;
using FluentAssertions;

namespace ContentService.Tests.Unit.Repositories;

public class ProblemRepositoryTests : IDisposable
{
    private readonly ContentDbContext _context;
    private readonly ProblemRepository _repository;

    public ProblemRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"ProblemRepositoryTests_{Guid.NewGuid()}");
        _repository = new ProblemRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnProblem()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(problem.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(problem.Id);
        result.Title.Should().Be(problem.Title);
        result.Slug.Should().Be(problem.Slug);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeRelated_ShouldIncludeNavigationProperties()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var tag = TestDataBuilder.CreateProblemTag(problem.Id, "dynamic-programming");
        await _context.ProblemTags.AddAsync(tag);

        var testCase = TestDataBuilder.CreateTestCase(problemId: problem.Id);
        await _context.TestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(problem.Id, includeRelated: true);

        result.Should().NotBeNull();
        result!.Tags.Should().HaveCount(1);
        result.TestCases.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBySlugAsync_WithExistingSlug_ShouldReturnProblem()
    {
        var problem = TestDataBuilder.CreateProblem(slug: "unique-slug");
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.GetBySlugAsync("unique-slug");

        result.Should().NotBeNull();
        result!.Slug.Should().Be("unique-slug");
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistingSlug_ShouldReturnNull()
    {
        var result = await _repository.GetBySlugAsync("non-existing-slug");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyPublicActiveProblems()
    {
        var publicProblem = TestDataBuilder.CreateProblem(slug: "public", visibility: ProblemVisibility.Public);
        var privateProblem = TestDataBuilder.CreateProblem(slug: "private", visibility: ProblemVisibility.Private);
        var inactiveProblem = TestDataBuilder.CreateProblem(slug: "inactive", isActive: false);

        await _context.Problems.AddRangeAsync(publicProblem, privateProblem, inactiveProblem);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(page: 1, pageSize: 10);

        result.Should().HaveCount(1);
        result.First().Slug.Should().Be("public");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            var problem = TestDataBuilder.CreateProblem(slug: $"problem-{i}");
            await _context.Problems.AddAsync(problem);
        }
        await _context.SaveChangesAsync();

        var page1 = await _repository.GetAllAsync(page: 1, pageSize: 2);
        var page2 = await _repository.GetAllAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WithSearchTerm_ShouldFilterByTitleOrDescription()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "unique-sum", title: "Unique Sum Problem");
        var problem2 = TestDataBuilder.CreateProblem(slug: "tree-traversal", title: "Tree Traversal");

        await _context.Problems.AddRangeAsync(problem1, problem2);
        await _context.SaveChangesAsync();

        var result = await _repository.SearchAsync("Unique", difficulty: null, tag: null, visibility: null, page: 1, pageSize: 10);

        result.Should().HaveCount(1);
        result.First().Title.Should().Contain("Unique");
    }

    [Fact]
    public async Task SearchAsync_WithDifficulty_ShouldFilterByDifficulty()
    {
        var easyProblem = TestDataBuilder.CreateProblem(slug: "easy", difficulty: Difficulty.Easy);
        var hardProblem = TestDataBuilder.CreateProblem(slug: "hard", difficulty: Difficulty.Hard);

        await _context.Problems.AddRangeAsync(easyProblem, hardProblem);
        await _context.SaveChangesAsync();

        var result = await _repository.SearchAsync(searchTerm: null, Difficulty.Hard, tag: null, visibility: null, page: 1, pageSize: 10);

        result.Should().HaveCount(1);
        result.First().Difficulty.Should().Be(Difficulty.Hard);
    }

    [Fact]
    public async Task SearchAsync_WithTag_ShouldFilterByTag()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "dp-problem");
        var problem2 = TestDataBuilder.CreateProblem(slug: "graph-problem");

        await _context.Problems.AddRangeAsync(problem1, problem2);
        await _context.SaveChangesAsync();

        var tag = TestDataBuilder.CreateProblemTag(problem1.Id, "dynamic-programming");
        await _context.ProblemTags.AddAsync(tag);
        await _context.SaveChangesAsync();

        var result = await _repository.SearchAsync(searchTerm: null, difficulty: null, "dynamic-programming", visibility: null, page: 1, pageSize: 10);

        result.Should().HaveCount(1);
        result.First().Slug.Should().Be("dp-problem");
    }

    [Fact]
    public async Task SearchAsync_WithVisibility_ShouldFilterByVisibility()
    {
        var publicProblem = TestDataBuilder.CreateProblem(slug: "public", visibility: ProblemVisibility.Public);
        var privateProblem = TestDataBuilder.CreateProblem(slug: "private", visibility: ProblemVisibility.Private);

        await _context.Problems.AddRangeAsync(publicProblem, privateProblem);
        await _context.SaveChangesAsync();

        var result = await _repository.SearchAsync(searchTerm: null, difficulty: null, tag: null, ProblemVisibility.Private, page: 1, pageSize: 10);

        result.Should().HaveCount(1);
        result.First().Visibility.Should().Be(ProblemVisibility.Private);
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnPublicActiveProblemsCount()
    {
        var publicProblem1 = TestDataBuilder.CreateProblem(slug: "public-1");
        var publicProblem2 = TestDataBuilder.CreateProblem(slug: "public-2");
        var privateProblem = TestDataBuilder.CreateProblem(slug: "private", visibility: ProblemVisibility.Private);

        await _context.Problems.AddRangeAsync(publicProblem1, publicProblem2, privateProblem);
        await _context.SaveChangesAsync();

        var count = await _repository.GetTotalCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetSearchCountAsync_WithFilters_ShouldReturnCorrectCount()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "easy-1", difficulty: Difficulty.Easy);
        var problem2 = TestDataBuilder.CreateProblem(slug: "easy-2", difficulty: Difficulty.Easy);
        var problem3 = TestDataBuilder.CreateProblem(slug: "hard-1", difficulty: Difficulty.Hard);

        await _context.Problems.AddRangeAsync(problem1, problem2, problem3);
        await _context.SaveChangesAsync();

        var count = await _repository.GetSearchCountAsync(searchTerm: null, Difficulty.Easy, tag: null, visibility: null);

        count.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidProblem_ShouldCreateSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem(slug: "new-problem");

        var result = await _repository.CreateAsync(problem);

        result.Id.Should().BeGreaterThan(0);
        result.Slug.Should().Be("new-problem");

        var savedProblem = await _context.Problems.FindAsync(result.Id);
        savedProblem.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingProblem_ShouldUpdateSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        problem.Title = "Updated Title";
        var result = await _repository.UpdateAsync(problem);

        result.Title.Should().Be("Updated Title");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProblem_ShouldMarkAsInactive()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(problem.Id);

        result.Should().BeTrue();

        var deletedProblem = await _context.Problems.FindAsync(problem.Id);
        deletedProblem!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingProblem_ShouldReturnFalse()
    {
        var result = await _repository.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingActiveProblem_ShouldReturnTrue()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(problem.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithInactiveProblem_ShouldReturnFalse()
    {
        var problem = TestDataBuilder.CreateProblem(isActive: false);
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(problem.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SlugExistsAsync_WithExistingSlug_ShouldReturnTrue()
    {
        var problem = TestDataBuilder.CreateProblem(slug: "unique-slug");
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.SlugExistsAsync("unique-slug");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SlugExistsAsync_WithNonExistingSlug_ShouldReturnFalse()
    {
        var result = await _repository.SlugExistsAsync("non-existing");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByAuthorAsync_ShouldReturnProblemsForAuthor()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "author1-prob1", authorId: 1);
        var problem2 = TestDataBuilder.CreateProblem(slug: "author1-prob2", authorId: 1);
        var problem3 = TestDataBuilder.CreateProblem(slug: "author2-prob1", authorId: 2);

        await _context.Problems.AddRangeAsync(problem1, problem2, problem3);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByAuthorAsync(authorId: 1, page: 1, pageSize: 10);

        result.Should().HaveCount(2);
        result.All(p => p.AuthorId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task IncrementViewCountAsync_ShouldIncreaseViewCount()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var initialViewCount = problem.ViewCount;

        await _repository.IncrementViewCountAsync(problem.Id);

        var updatedProblem = await _context.Problems.FindAsync(problem.Id);
        updatedProblem!.ViewCount.Should().Be(initialViewCount + 1);
    }

    [Fact]
    public async Task UpdateStatisticsAsync_ShouldUpdateSubmissionAndAcceptanceData()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        await _repository.UpdateStatisticsAsync(problem.Id, submissionCount: 100, acceptedCount: 75);

        var updatedProblem = await _context.Problems.FindAsync(problem.Id);
        updatedProblem!.SubmissionCount.Should().Be(100);
        updatedProblem.AcceptedCount.Should().Be(75);
        updatedProblem.AcceptanceRate.Should().Be(75.00m);
    }

    [Fact]
    public async Task UpdateStatisticsAsync_WithZeroSubmissions_ShouldSetAcceptanceRateToZero()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        await _repository.UpdateStatisticsAsync(problem.Id, submissionCount: 0, acceptedCount: 0);

        var updatedProblem = await _context.Problems.FindAsync(problem.Id);
        updatedProblem!.AcceptanceRate.Should().Be(0);
    }

    [Fact]
    public async Task GetAllTagsAsync_ShouldReturnDistinctTagsSorted()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "problem-1");
        var problem2 = TestDataBuilder.CreateProblem(slug: "problem-2");
        await _context.Problems.AddRangeAsync(problem1, problem2);
        await _context.SaveChangesAsync();

        var tag1 = TestDataBuilder.CreateProblemTag(problem1.Id, "dynamic-programming");
        var tag2 = TestDataBuilder.CreateProblemTag(problem1.Id);
        var tag3 = TestDataBuilder.CreateProblemTag(problem2.Id);
        await _context.ProblemTags.AddRangeAsync(tag1, tag2, tag3);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllTagsAsync();

        result.Should().HaveCount(2);
        result.Should().Contain("array");
        result.Should().Contain("dynamic-programming");
        result.Should().BeInAscendingOrder();
    }
}
