namespace ContentService.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class EditorialServiceTests
{
    private readonly Mock<IEditorialRepository> _editorialRepositoryMock;
    private readonly Mock<IProblemRepository> _problemRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<EditorialService>> _loggerMock;
    private readonly EditorialService _service;

    public EditorialServiceTests()
    {
        _editorialRepositoryMock = new Mock<IEditorialRepository>();
        _problemRepositoryMock = new Mock<IProblemRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<EditorialService>>();

        _service = new EditorialService(
            _editorialRepositoryMock.Object,
            _problemRepositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetEditorialAsync_WithValidProblemId_ShouldReturnEditorial()
    {
        var problemId = 10L;
        var expectedEditorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Editorial content",
            Approach = "Dynamic programming",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = 100L,
            IsPublished = true
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(expectedEditorial);

        var result = await _service.GetEditorialAsync(problemId);

        Assert.NotNull(result);
        Assert.Equal(problemId, result.ProblemId);
        Assert.Equal("Editorial content", result.Content);
    }

    [Fact]
    public async Task GetEditorialAsync_WithNonExistentProblem_ShouldReturnNull()
    {
        var problemId = 999L;

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync((Editorial?)null);

        var result = await _service.GetEditorialAsync(problemId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEditorialByIdAsync_WithValidId_ShouldReturnEditorial()
    {
        var editorialId = 1L;
        var expectedEditorial = new Editorial
        {
            Id = editorialId,
            ProblemId = 10L,
            Content = "Editorial content",
            Approach = "Greedy",
            TimeComplexity = "O(n log n)",
            SpaceComplexity = "O(n)",
            AuthorId = 100L
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByIdAsync(editorialId))
            .ReturnsAsync(expectedEditorial);

        var result = await _service.GetEditorialByIdAsync(editorialId);

        Assert.NotNull(result);
        Assert.Equal(editorialId, result.Id);
    }

    [Fact]
    public async Task CreateEditorialAsync_WithValidData_ShouldCreateEditorial()
    {
        var problemId = 10L;
        var content = "This is the editorial";
        var approach = "Dynamic Programming";
        var complexity = "O(n^2)";
        var authorId = 100L;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Medium,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync((Editorial?)null);

        _editorialRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Editorial>()))
            .ReturnsAsync((Editorial e) =>
            {
                e.Id = 1L;
                return e;
            });

        var result = await _service.CreateEditorialAsync(problemId, content, approach, complexity, authorId);

        Assert.NotNull(result);
        Assert.Equal(problemId, result.ProblemId);
        Assert.Equal(content, result.Content);
        Assert.Equal(approach, result.Approach);
        Assert.Equal(complexity, result.TimeComplexity);
        Assert.Equal(complexity, result.SpaceComplexity);
        Assert.Equal(authorId, result.AuthorId);
        Assert.False(result.IsPublished);

        _editorialRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Editorial>()), Times.Once);
    }

    [Fact]
    public async Task CreateEditorialAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var problemId = 999L;
        var authorId = 100L;

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync((Problem?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateEditorialAsync(problemId, "content", "approach", "complexity", authorId));
    }

    [Fact]
    public async Task CreateEditorialAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Medium,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.CreateEditorialAsync(problemId, "content", "approach", "complexity", differentUserId));
    }

    [Fact]
    public async Task CreateEditorialAsync_WhenEditorialAlreadyExists_ShouldThrowException()
    {
        var problemId = 10L;
        var authorId = 100L;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Medium,
            AuthorId = authorId
        };

        var existingEditorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Existing editorial",
            Approach = "DP",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(n)",
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(existingEditorial);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateEditorialAsync(problemId, "content", "approach", "complexity", authorId));
    }

    [Fact]
    public async Task UpdateEditorialAsync_WithValidData_ShouldUpdateEditorial()
    {
        var problemId = 10L;
        var userId = 100L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Old content",
            Approach = "Old approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId,
            IsPublished = false
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        _editorialRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Editorial>()))
            .ReturnsAsync((Editorial e) => e);

        var result = await _service.UpdateEditorialAsync(
            problemId,
            "New content",
            "New approach",
            "O(n log n)",
            userId);

        Assert.Equal("New content", result.Content);
        Assert.Equal("New approach", result.Approach);
        Assert.Equal("O(n log n)", result.TimeComplexity);
        Assert.Equal("O(n log n)", result.SpaceComplexity);

        _editorialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Editorial>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEditorialAsync_WithNonExistentEditorial_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync((Editorial?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateEditorialAsync(problemId, "content", "approach", "complexity", userId));
    }

    [Fact]
    public async Task UpdateEditorialAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = authorId
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UpdateEditorialAsync(problemId, "new content", "new approach", "O(1)", differentUserId));
    }

    [Fact]
    public async Task PublishEditorialAsync_WithValidData_ShouldPublishAndRaiseEvent()
    {
        var problemId = 10L;
        var userId = 100L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId,
            IsPublished = false
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        _editorialRepositoryMock
            .Setup(r => r.PublishAsync(editorial.Id))
            .ReturnsAsync(true);

        _eventPublisherMock
            .Setup(e => e.PublishEditorialPublishedAsync(
                problemId,
                editorial.Id,
                userId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.PublishEditorialAsync(problemId, userId);

        _editorialRepositoryMock.Verify(r => r.PublishAsync(editorial.Id), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishEditorialPublishedAsync(problemId, editorial.Id, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishEditorialAsync_WithNonExistentEditorial_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync((Editorial?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.PublishEditorialAsync(problemId, userId));
    }

    [Fact]
    public async Task PublishEditorialAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = authorId,
            IsPublished = false
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.PublishEditorialAsync(problemId, differentUserId));
    }

    [Fact]
    public async Task PublishEditorialAsync_WhenAlreadyPublished_ShouldThrowException()
    {
        var problemId = 10L;
        var userId = 100L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.PublishEditorialAsync(problemId, userId));
    }

    [Fact]
    public async Task UnpublishEditorialAsync_WithValidData_ShouldUnpublish()
    {
        var problemId = 10L;
        var userId = 100L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        _editorialRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Editorial>()))
            .ReturnsAsync((Editorial e) => e);

        await _service.UnpublishEditorialAsync(problemId, userId);

        Assert.False(editorial.IsPublished);
        Assert.Null(editorial.PublishedAt);

        _editorialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Editorial>()), Times.Once);
    }

    [Fact]
    public async Task UnpublishEditorialAsync_WhenNotPublished_ShouldThrowException()
    {
        var problemId = 10L;
        var userId = 100L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId,
            IsPublished = false
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UnpublishEditorialAsync(problemId, userId));
    }

    [Fact]
    public async Task DeleteEditorialAsync_WithValidData_ShouldDeleteEditorial()
    {
        var problemId = 10L;
        var userId = 100L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        _editorialRepositoryMock
            .Setup(r => r.DeleteAsync(editorial.Id))
            .ReturnsAsync(true);

        await _service.DeleteEditorialAsync(problemId, userId);

        _editorialRepositoryMock.Verify(r => r.DeleteAsync(editorial.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteEditorialAsync_WithNonExistentEditorial_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync((Editorial?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.DeleteEditorialAsync(problemId, userId));
    }

    [Fact]
    public async Task DeleteEditorialAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = authorId
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.DeleteEditorialAsync(problemId, differentUserId));
    }

    [Fact]
    public async Task EditorialExistsAsync_WithExistingEditorial_ShouldReturnTrue()
    {
        var problemId = 10L;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = 100L
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        var result = await _service.EditorialExistsAsync(problemId);

        Assert.True(result);
    }

    [Fact]
    public async Task EditorialExistsAsync_WithNonExistentEditorial_ShouldReturnFalse()
    {
        var problemId = 999L;

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync((Editorial?)null);

        var result = await _service.EditorialExistsAsync(problemId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorOrAdminAsync_WithAdmin_ShouldReturnTrue()
    {
        var problemId = 10L;
        var userId = 999L;
        var isAdmin = true;

        var result = await _service.IsAuthorOrAdminAsync(problemId, userId, isAdmin);

        Assert.True(result);
        _editorialRepositoryMock.Verify(r => r.GetByProblemIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task IsAuthorOrAdminAsync_WithAuthor_ShouldReturnTrue()
    {
        var problemId = 10L;
        var userId = 100L;
        var isAdmin = false;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = userId
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        var result = await _service.IsAuthorOrAdminAsync(problemId, userId, isAdmin);

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorOrAdminAsync_WithNeitherAuthorNorAdmin_ShouldReturnFalse()
    {
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;
        var isAdmin = false;
        var editorial = new Editorial
        {
            Id = 1L,
            ProblemId = problemId,
            Content = "Content",
            Approach = "Approach",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(1)",
            AuthorId = authorId
        };

        _editorialRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(editorial);

        var result = await _service.IsAuthorOrAdminAsync(problemId, differentUserId, isAdmin);

        Assert.False(result);
    }
}
