using ContentService.Data;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContentService.Tests.Services;

public class ProblemServiceTests : IDisposable
{
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IProblemRepository> _problemRepositoryMock;
    private readonly ContentDbContext _dbContext;
    private readonly ProblemService _service;

    public ProblemServiceTests()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProblemServiceTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ContentDbContext(options);
        _problemRepositoryMock = new Mock<IProblemRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        var loggerMock = new Mock<ILogger<ProblemService>>();

        _service = new ProblemService(
            _problemRepositoryMock.Object,
            _dbContext,
            _eventPublisherMock.Object,
            loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task GetProblemAsync_WithValidId_ShouldReturnProblem()
    {
        var problemId = 1L;
        var expectedProblem = new Problem
        {
            Id = problemId,
            Title = "Two Sum",
            Slug = "two-sum",
            Description = "Find two numbers that add up to target",
            InputFormat = "Two integers",
            OutputFormat = "One integer",
            Constraints = "1 <= n <= 100",
            Difficulty = Difficulty.Easy,
            AuthorId = 100L
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, true))
            .ReturnsAsync(expectedProblem);

        var result = await _service.GetProblemAsync(problemId);

        Assert.NotNull(result);
        Assert.Equal(problemId, result.Id);
        Assert.Equal("Two Sum", result.Title);
        _problemRepositoryMock.Verify(r => r.GetByIdAsync(problemId, true), Times.Once);
    }

    [Fact]
    public async Task GetProblemAsync_WithInvalidId_ShouldReturnNull()
    {
        var problemId = 999L;

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, true))
            .ReturnsAsync((Problem?)null);

        var result = await _service.GetProblemAsync(problemId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProblemBySlugAsync_WithValidSlug_ShouldReturnProblem()
    {
        var slug = "two-sum";
        var expectedProblem = new Problem
        {
            Id = 1L,
            Title = "Two Sum",
            Slug = slug,
            Description = "Find two numbers that add up to target",
            InputFormat = "Two integers",
            OutputFormat = "One integer",
            Constraints = "1 <= n <= 100",
            Difficulty = Difficulty.Easy,
            AuthorId = 100L
        };

        _problemRepositoryMock
            .Setup(r => r.GetBySlugAsync(slug, true))
            .ReturnsAsync(expectedProblem);

        var result = await _service.GetProblemBySlugAsync(slug);

        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal("Two Sum", result.Title);
    }

    [Fact]
    public async Task GetProblemsAsync_ShouldReturnPaginatedProblems()
    {
        var page = 1;
        var pageSize = 10;
        var problems = new List<Problem>
        {
            new()
            {
                Id = 1, Title = "Problem 1", Slug = "problem-1", Description = "Desc 1", InputFormat = "Input 1", OutputFormat = "Output 1",
                Constraints = "Constraints 1", Difficulty = Difficulty.Easy, AuthorId = 100L
            },
            new()
            {
                Id = 2, Title = "Problem 2", Slug = "problem-2", Description = "Desc 2", InputFormat = "Input 2", OutputFormat = "Output 2",
                Constraints = "Constraints 2", Difficulty = Difficulty.Medium, AuthorId = 100L
            }
        };

        _problemRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<Difficulty?>(),
                It.IsAny<string>(),
                It.IsAny<ProblemVisibility?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(problems);

        _problemRepositoryMock
            .Setup(r => r.GetTotalCountAsync())
            .ReturnsAsync(2);

        var result = await _service.GetProblemsAsync(page, pageSize);

        Assert.Equal(expected: 2, result.Count());
    }

    [Fact]
    public async Task SearchProblemsAsync_WithFilters_ShouldReturnFilteredProblems()
    {
        var searchTerm = "sum";
        var difficulty = Difficulty.Easy;
        var tags = new List<string> { "array", "hash-table" };
        var page = 1;
        var pageSize = 10;

        var expectedProblems = new List<Problem>
        {
            new()
            {
                Id = 1, Title = "Two Sum", Slug = "two-sum", Description = "Desc", InputFormat = "Input", OutputFormat = "Output",
                Constraints = "Constraints", Difficulty = Difficulty.Easy, AuthorId = 100L
            }
        };

        _problemRepositoryMock
            .Setup(r => r.SearchAsync(searchTerm, difficulty, "array", null, page, pageSize))
            .ReturnsAsync(expectedProblems);

        var result = await _service.SearchProblemsAsync(searchTerm, difficulty, tags, page, pageSize);

        IEnumerable<Problem> collections = result as Problem[] ?? result.ToArray();
        Assert.Single(collections);
        Assert.Equal("Two Sum", collections.First().Title);
    }

    [Fact]
    public async Task CreateProblemAsync_WithValidData_ShouldCreateProblemAndPublishEvent()
    {
        var title = "Two Sum";
        var description = "Find two numbers";
        var difficulty = Difficulty.Easy;
        var timeLimit = 1000;
        var memoryLimit = 256;
        var tags = new List<string> { "Array", "Hash-Table" };
        var authorId = 100L;

        _problemRepositoryMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _problemRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Problem>()))
            .ReturnsAsync((Problem p) =>
            {
                p.Id = 1L;
                return p;
            });

        _eventPublisherMock
            .Setup(e => e.PublishProblemCreatedAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateProblemAsync(
            title,
            description,
            "Input format",
            "Output format",
            "Constraints",
            difficulty,
            timeLimit,
            memoryLimit,
            authorId,
            tags);

        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal("two-sum", result.Slug);
        Assert.Equal(Difficulty.Easy, result.Difficulty);
        Assert.Equal(authorId, result.AuthorId);
        Assert.Equal(ProblemVisibility.Public, result.Visibility);
        Assert.Equal(expected: 2, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Tag == "array");
        Assert.Contains(result.Tags, t => t.Tag == "hash-table");

        _problemRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Problem>()), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishProblemCreatedAsync(
                It.IsAny<long>(),
                title,
                "two-sum",
                authorId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateProblemAsync_WithDuplicateSlug_ShouldAppendCounter()
    {
        var title = "Two Sum";
        var description = "Find two numbers";
        var difficulty = Difficulty.Easy;
        var timeLimit = 1000;
        var memoryLimit = 256;
        var tags = new List<string> { "array" };
        var authorId = 100L;

        _problemRepositoryMock
            .SetupSequence(r => r.SlugExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _problemRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Problem>()))
            .ReturnsAsync((Problem p) =>
            {
                p.Id = 1L;
                return p;
            });

        var result = await _service.CreateProblemAsync(
            title,
            description,
            "Input format",
            "Output format",
            "Constraints",
            difficulty,
            timeLimit,
            memoryLimit,
            authorId,
            tags);

        Assert.Equal("two-sum-1", result.Slug);
    }

    [Fact]
    public async Task CreateProblemAsync_WithSpecialCharactersInTitle_ShouldGenerateCleanSlug()
    {
        var title = "Add Two Numbers #2 (Hard!)";
        var description = "Description";
        var difficulty = Difficulty.Hard;
        var timeLimit = 2000;
        var memoryLimit = 512;
        var tags = new List<string> { "linked-list" };
        var authorId = 100L;

        _problemRepositoryMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _problemRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Problem>()))
            .ReturnsAsync((Problem p) =>
            {
                p.Id = 1L;
                return p;
            });

        var result = await _service.CreateProblemAsync(
            title,
            description,
            "Input format",
            "Output format",
            "Constraints",
            difficulty,
            timeLimit,
            memoryLimit,
            authorId,
            tags);

        Assert.Equal("add-two-numbers-2-hard", result.Slug);
    }

    [Fact]
    public async Task UpdateProblemAsync_WithValidData_ShouldUpdateProblemAndPublishEvent()
    {
        var problemId = 1L;
        var userId = 100L;
        var existingProblem = new Problem
        {
            Id = problemId,
            Title = "Old Title",
            Slug = "old-title",
            Description = "Old description",
            InputFormat = "Old input",
            OutputFormat = "Old output",
            Constraints = "Old constraints",
            Difficulty = Difficulty.Easy,
            TimeLimit = 1000,
            MemoryLimit = 256,
            AuthorId = userId
        };
        existingProblem.Tags.Add(new ProblemTag { Tag = "old-tag" });

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, true))
            .ReturnsAsync(existingProblem);

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(existingProblem);

        _problemRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Problem>()))
            .ReturnsAsync((Problem p) => p);

        _eventPublisherMock
            .Setup(e => e.PublishProblemUpdatedAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProblemAsync(
            problemId,
            userId,
            "New Title",
            "New description",
            difficulty: Difficulty.Medium,
            timeLimit: 2000,
            memoryLimit: 512,
            tags: ["new-tag"]);

        Assert.Equal("New Title", result.Title);
        Assert.Equal("New description", result.Description);
        Assert.Equal(Difficulty.Medium, result.Difficulty);
        Assert.Equal(expected: 2000, result.TimeLimit);
        Assert.Equal(expected: 512, result.MemoryLimit);
        Assert.Contains(result.Tags, t => t.Tag == "new-tag");

        _problemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Problem>()), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishProblemUpdatedAsync(problemId, "New Title", userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProblemAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, true))
            .ReturnsAsync((Problem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.UpdateProblemAsync(
                problemId,
                userId,
                "Title",
                "Description",
                difficulty: Difficulty.Easy,
                timeLimit: 1000,
                memoryLimit: 256,
                tags: ["tag"]));
    }

    [Fact]
    public async Task UpdateProblemAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var existingProblem = new Problem
        {
            Id = problemId,
            Title = "Title",
            Slug = "title",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, true))
            .ReturnsAsync(existingProblem);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UpdateProblemAsync(
                problemId,
                differentUserId,
                "New Title",
                "New Description",
                difficulty: Difficulty.Easy,
                timeLimit: 1000,
                memoryLimit: 256,
                tags: ["tag"]));
    }

    [Fact]
    public async Task DeleteProblemAsync_WithValidData_ShouldDeleteProblemAndPublishEvent()
    {
        var problemId = 1L;
        var userId = 100L;
        var existingProblem = new Problem
        {
            Id = problemId,
            Title = "Problem to Delete",
            Slug = "problem-to-delete",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(existingProblem);

        _problemRepositoryMock
            .Setup(r => r.DeleteAsync(problemId))
            .ReturnsAsync(true);

        _eventPublisherMock
            .Setup(e => e.PublishProblemDeletedAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteProblemAsync(problemId, userId);

        _problemRepositoryMock.Verify(r => r.DeleteAsync(problemId), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishProblemDeletedAsync(problemId, "Problem to Delete", userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteProblemAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync((Problem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.DeleteProblemAsync(problemId, userId));
    }

    [Fact]
    public async Task DeleteProblemAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var existingProblem = new Problem
        {
            Id = problemId,
            Title = "Problem",
            Slug = "problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(existingProblem);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.DeleteProblemAsync(problemId, differentUserId));
    }

    [Fact]
    public async Task IncrementViewCountAsync_ShouldCallRepository()
    {
        var problemId = 1L;

        _problemRepositoryMock
            .Setup(r => r.IncrementViewCountAsync(problemId))
            .Returns(Task.CompletedTask);

        await _service.IncrementViewCountAsync(problemId);

        _problemRepositoryMock.Verify(r => r.IncrementViewCountAsync(problemId), Times.Once);
    }

    [Fact]
    public async Task UpdateStatisticsAsync_ShouldCallRepository()
    {
        var problemId = 1L;
        var submissionCount = 100;
        var acceptedCount = 50;

        _problemRepositoryMock
            .Setup(r => r.UpdateStatisticsAsync(problemId, submissionCount, acceptedCount))
            .Returns(Task.CompletedTask);

        await _service.UpdateStatisticsAsync(problemId, submissionCount, acceptedCount);

        _problemRepositoryMock.Verify(
            r => r.UpdateStatisticsAsync(problemId, submissionCount, acceptedCount),
            Times.Once);
    }

    [Fact]
    public async Task GetProblemsByAuthorAsync_ShouldReturnAuthorProblems()
    {
        var authorId = 100L;
        var expectedProblems = new List<Problem>
        {
            new()
            {
                Id = 1, Title = "Problem 1", Slug = "problem-1", Description = "Desc 1", InputFormat = "Input 1", OutputFormat = "Output 1",
                Constraints = "Constraints 1", Difficulty = Difficulty.Easy, AuthorId = authorId
            },
            new()
            {
                Id = 2, Title = "Problem 2", Slug = "problem-2", Description = "Desc 2", InputFormat = "Input 2", OutputFormat = "Output 2",
                Constraints = "Constraints 2", Difficulty = Difficulty.Medium, AuthorId = authorId
            }
        };

        _problemRepositoryMock
            .Setup(r => r.GetByAuthorAsync(authorId, 1, int.MaxValue))
            .ReturnsAsync(expectedProblems);

        var result = await _service.GetProblemsByAuthorAsync(authorId);

        IEnumerable<Problem> enumerates = result as Problem[] ?? result.ToArray();
        Assert.Equal(expected: 2, enumerates.Count());
        Assert.All(enumerates, p => Assert.Equal(authorId, p.AuthorId));
    }

    [Fact]
    public async Task ProblemExistsAsync_WithExistingProblem_ShouldReturnTrue()
    {
        var problemId = 1L;

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(problemId))
            .ReturnsAsync(true);

        var result = await _service.ProblemExistsAsync(problemId);

        Assert.True(result);
    }

    [Fact]
    public async Task ProblemExistsAsync_WithNonExistentProblem_ShouldReturnFalse()
    {
        var problemId = 999L;

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(problemId))
            .ReturnsAsync(false);

        var result = await _service.ProblemExistsAsync(problemId);

        Assert.False(result);
    }

    [Fact]
    public async Task SlugExistsAsync_WithExistingSlug_ShouldReturnTrue()
    {
        var slug = "two-sum";

        _problemRepositoryMock
            .Setup(r => r.SlugExistsAsync(slug))
            .ReturnsAsync(true);

        var result = await _service.SlugExistsAsync(slug);

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorOrAdminAsync_WithAdmin_ShouldReturnTrue()
    {
        var problemId = 1L;
        var userId = 999L;
        var isAdmin = true;

        var result = await _service.IsAuthorOrAdminAsync(problemId, userId, isAdmin);

        Assert.True(result);
        _problemRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task IsAuthorOrAdminAsync_WithAuthor_ShouldReturnTrue()
    {
        var problemId = 1L;
        var userId = 100L;
        var isAdmin = false;
        var problem = new Problem
        {
            Id = problemId,
            Title = "Problem",
            Slug = "problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        var result = await _service.IsAuthorOrAdminAsync(problemId, userId, isAdmin);

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorOrAdminAsync_WithNeitherAuthorNorAdmin_ShouldReturnFalse()
    {
        var problemId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var isAdmin = false;
        var problem = new Problem
        {
            Id = problemId,
            Title = "Problem",
            Slug = "problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        var result = await _service.IsAuthorOrAdminAsync(problemId, differentUserId, isAdmin);

        Assert.False(result);
    }
}
