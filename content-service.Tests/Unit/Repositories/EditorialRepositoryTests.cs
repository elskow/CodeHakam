using ContentService.Data;
using ContentService.Repositories.Implementations;
using ContentService.Tests.Helpers;
using FluentAssertions;

namespace ContentService.Tests.Unit.Repositories;

public class EditorialRepositoryTests : IDisposable
{
    private readonly ContentDbContext _context;
    private readonly EditorialRepository _repository;

    public EditorialRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"EditorialRepositoryTests_{Guid.NewGuid()}");
        _repository = new EditorialRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEditorial()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(editorial.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(editorial.Id);
        result.ProblemId.Should().Be(problem.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProblemIdAsync_WithExistingProblem_ShouldReturnEditorial()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByProblemIdAsync(problem.Id);

        result.Should().NotBeNull();
        result!.ProblemId.Should().Be(problem.Id);
    }

    [Fact]
    public async Task GetByProblemIdAsync_WithNonExistingProblem_ShouldReturnNull()
    {
        var result = await _repository.GetByProblemIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublishedEditorialsAsync_ShouldReturnOnlyPublishedEditorials()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "problem-1");
        var problem2 = TestDataBuilder.CreateProblem(slug: "problem-2");
        var problem3 = TestDataBuilder.CreateProblem(slug: "problem-3");
        await _context.Problems.AddRangeAsync(problem1, problem2, problem3);
        await _context.SaveChangesAsync();

        var publishedEditorial1 = TestDataBuilder.CreateEditorial(problemId: problem1.Id, isPublished: true);
        var publishedEditorial2 = TestDataBuilder.CreateEditorial(problemId: problem2.Id, isPublished: true);
        var unpublishedEditorial = TestDataBuilder.CreateEditorial(problemId: problem3.Id, isPublished: false);

        await _context.Editorials.AddRangeAsync(publishedEditorial1, publishedEditorial2, unpublishedEditorial);
        await _context.SaveChangesAsync();

        var result = await _repository.GetPublishedEditorialsAsync(page: 1, pageSize: 10);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.IsPublished);
    }

    [Fact]
    public async Task GetPublishedEditorialsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            var problem = TestDataBuilder.CreateProblem(slug: $"problem-{i}");
            await _context.Problems.AddAsync(problem);
            await _context.SaveChangesAsync();

            var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id, isPublished: true);
            await _context.Editorials.AddAsync(editorial);
        }
        await _context.SaveChangesAsync();

        var page1 = await _repository.GetPublishedEditorialsAsync(page: 1, pageSize: 2);
        var page2 = await _repository.GetPublishedEditorialsAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidEditorial_ShouldCreateSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id);

        var result = await _repository.CreateAsync(editorial);

        result.Id.Should().BeGreaterThan(0);
        result.ProblemId.Should().Be(problem.Id);

        var savedEditorial = await _context.Editorials.FindAsync(result.Id);
        savedEditorial.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEditorial_ShouldUpdateSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        editorial.Content = "Updated content";
        var result = await _repository.UpdateAsync(editorial);

        result.Content.Should().Be("Updated content");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEditorial_ShouldDeleteSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(editorial.Id);

        result.Should().BeTrue();

        var deletedEditorial = await _context.Editorials.FindAsync(editorial.Id);
        deletedEditorial.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingEditorial_ShouldReturnFalse()
    {
        var result = await _repository.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task PublishAsync_WithExistingEditorial_ShouldPublishSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id, isPublished: false);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        var result = await _repository.PublishAsync(editorial.Id);

        result.Should().BeTrue();

        var publishedEditorial = await _context.Editorials.FindAsync(editorial.Id);
        publishedEditorial!.IsPublished.Should().BeTrue();
        publishedEditorial.PublishedAt.Should().NotBeNull();
        publishedEditorial.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PublishAsync_WithNonExistingEditorial_ShouldReturnFalse()
    {
        var result = await _repository.PublishAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnpublishAsync_WithPublishedEditorial_ShouldUnpublishSuccessfully()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id, isPublished: true);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        var result = await _repository.UnpublishAsync(editorial.Id);

        result.Should().BeTrue();

        var unpublishedEditorial = await _context.Editorials.FindAsync(editorial.Id);
        unpublishedEditorial!.IsPublished.Should().BeFalse();
        unpublishedEditorial.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnpublishAsync_WithNonExistingEditorial_ShouldReturnFalse()
    {
        var result = await _repository.UnpublishAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsForProblemAsync_WithExistingEditorial_ShouldReturnTrue()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var editorial = TestDataBuilder.CreateEditorial(problemId: problem.Id);
        await _context.Editorials.AddAsync(editorial);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsForProblemAsync(problem.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsForProblemAsync_WithNonExistingEditorial_ShouldReturnFalse()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsForProblemAsync(problem.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublishedCountAsync_ShouldReturnCorrectCount()
    {
        var problem1 = TestDataBuilder.CreateProblem(slug: "problem-1");
        var problem2 = TestDataBuilder.CreateProblem(slug: "problem-2");
        var problem3 = TestDataBuilder.CreateProblem(slug: "problem-3");
        await _context.Problems.AddRangeAsync(problem1, problem2, problem3);
        await _context.SaveChangesAsync();

        var publishedEditorial1 = TestDataBuilder.CreateEditorial(problemId: problem1.Id, isPublished: true);
        var publishedEditorial2 = TestDataBuilder.CreateEditorial(problemId: problem2.Id, isPublished: true);
        var unpublishedEditorial = TestDataBuilder.CreateEditorial(problemId: problem3.Id, isPublished: false);

        await _context.Editorials.AddRangeAsync(publishedEditorial1, publishedEditorial2, unpublishedEditorial);
        await _context.SaveChangesAsync();

        var count = await _repository.GetPublishedCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetPublishedCountAsync_WithNoPublishedEditorials_ShouldReturnZero()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var unpublishedEditorial = TestDataBuilder.CreateEditorial(problemId: problem.Id, isPublished: false);
        await _context.Editorials.AddAsync(unpublishedEditorial);
        await _context.SaveChangesAsync();

        var count = await _repository.GetPublishedCountAsync();

        count.Should().Be(0);
    }
}
