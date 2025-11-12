namespace ContentService.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
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

public class ProblemListServiceTests
{
    private readonly Mock<IProblemListRepository> _problemListRepositoryMock;
    private readonly Mock<IProblemRepository> _problemRepositoryMock;
    private readonly Mock<ILogger<ProblemListService>> _loggerMock;
    private readonly ProblemListService _service;

    public ProblemListServiceTests()
    {
        _problemListRepositoryMock = new Mock<IProblemListRepository>();
        _problemRepositoryMock = new Mock<IProblemRepository>();
        _loggerMock = new Mock<ILogger<ProblemListService>>();

        _service = new ProblemListService(
            _problemListRepositoryMock.Object,
            _problemRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetProblemListAsync_WithValidId_ShouldReturnProblemList()
    {
        var listId = 1L;
        var expectedList = new ProblemList
        {
            Id = listId,
            Title = "Top Interview Questions",
            Description = "Most commonly asked questions",
            OwnerId = 100L,
            ProblemIds = new long[] { 1, 2, 3 },
            IsPublic = true
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(expectedList);

        var result = await _service.GetProblemListAsync(listId);

        Assert.NotNull(result);
        Assert.Equal(listId, result.Id);
        Assert.Equal("Top Interview Questions", result.Title);
        Assert.Equal(3, result.ProblemIds.Length);
    }

    [Fact]
    public async Task GetProblemListAsync_WithInvalidId_ShouldReturnNull()
    {
        var listId = 999L;

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync((ProblemList?)null);

        var result = await _service.GetProblemListAsync(listId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProblemListsByOwnerAsync_ShouldReturnOwnerLists()
    {
        var ownerId = 100L;
        var page = 1;
        var pageSize = 10;
        var expectedLists = new List<ProblemList>
        {
            new ProblemList { Id = 1, Title = "List 1", Description = "Desc 1", OwnerId = ownerId, ProblemIds = new long[] { 1, 2 }, IsPublic = true },
            new ProblemList { Id = 2, Title = "List 2", Description = "Desc 2", OwnerId = ownerId, ProblemIds = new long[] { 3, 4 }, IsPublic = false }
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByOwnerAsync(ownerId, page, pageSize))
            .ReturnsAsync(expectedLists);

        var result = await _service.GetProblemListsByOwnerAsync(ownerId, page, pageSize);

        Assert.Equal(2, result.Count());
        Assert.All(result, list => Assert.Equal(ownerId, list.OwnerId));
    }

    [Fact]
    public async Task GetPublicListsAsync_ShouldReturnPublicLists()
    {
        var page = 1;
        var pageSize = 10;
        var expectedLists = new List<ProblemList>
        {
            new ProblemList { Id = 1, Title = "Public List 1", Description = "Desc 1", OwnerId = 100L, ProblemIds = new long[] { 1, 2 }, IsPublic = true },
            new ProblemList { Id = 2, Title = "Public List 2", Description = "Desc 2", OwnerId = 101L, ProblemIds = new long[] { 3, 4 }, IsPublic = true }
        };

        _problemListRepositoryMock
            .Setup(r => r.GetPublicListsAsync(page, pageSize))
            .ReturnsAsync(expectedLists);

        var result = await _service.GetPublicListsAsync(page, pageSize);

        Assert.Equal(2, result.Count());
        Assert.All(result, list => Assert.True(list.IsPublic));
    }

    [Fact]
    public async Task CreateProblemListAsync_WithValidData_ShouldCreateList()
    {
        var name = "My Problem List";
        var description = "A curated list of problems";
        var isPublic = true;
        var ownerId = 100L;
        var problemIds = new List<long> { 1L, 2L, 3L };

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<long>()))
            .ReturnsAsync(true);

        _problemListRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<ProblemList>()))
            .ReturnsAsync((ProblemList pl) =>
            {
                pl.Id = 1L;
                return pl;
            });

        var result = await _service.CreateProblemListAsync(name, description, isPublic, ownerId, problemIds);

        Assert.NotNull(result);
        Assert.Equal(name, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(isPublic, result.IsPublic);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(3, result.ProblemIds.Length);

        _problemListRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<ProblemList>()), Times.Once);
    }

    [Fact]
    public async Task CreateProblemListAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var name = "My Problem List";
        var description = "Description";
        var isPublic = true;
        var ownerId = 100L;
        var problemIds = new List<long> { 1L, 999L };

        _problemRepositoryMock
            .SetupSequence(r => r.ExistsAsync(It.IsAny<long>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateProblemListAsync(name, description, isPublic, ownerId, problemIds));
    }

    [Fact]
    public async Task UpdateProblemListAsync_WithValidData_ShouldUpdateList()
    {
        var listId = 1L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Old Title",
            Description = "Old Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<long>()))
            .ReturnsAsync(true);

        _problemListRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProblemList>()))
            .ReturnsAsync((ProblemList pl) => pl);

        var result = await _service.UpdateProblemListAsync(
            listId,
            "New Title",
            "New Description",
            true,
            new List<long> { 3L, 4L, 5L },
            userId);

        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Description", result.Description);
        Assert.True(result.IsPublic);
        Assert.Equal(3, result.ProblemIds.Length);

        _problemListRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ProblemList>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProblemListAsync_WithNonExistentList_ShouldThrowException()
    {
        var listId = 999L;

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync((ProblemList?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateProblemListAsync(listId, "Title", "Desc", true, new List<long> { 1 }, 100L));
    }

    [Fact]
    public async Task UpdateProblemListAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var listId = 1L;
        var ownerId = 100L;
        var differentUserId = 200L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = ownerId,
            ProblemIds = new long[] { 1 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UpdateProblemListAsync(listId, "New Title", "New Desc", true, new List<long> { 1 }, differentUserId));
    }

    [Fact]
    public async Task DeleteProblemListAsync_WithValidData_ShouldDeleteList()
    {
        var listId = 1L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        _problemListRepositoryMock
            .Setup(r => r.DeleteAsync(listId))
            .ReturnsAsync(true);

        await _service.DeleteProblemListAsync(listId, userId);

        _problemListRepositoryMock.Verify(r => r.DeleteAsync(listId), Times.Once);
    }

    [Fact]
    public async Task DeleteProblemListAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var listId = 1L;
        var ownerId = 100L;
        var differentUserId = 200L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = ownerId,
            ProblemIds = new long[] { 1 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.DeleteProblemListAsync(listId, differentUserId));
    }

    [Fact]
    public async Task AddProblemToListAsync_WithValidData_ShouldAddProblem()
    {
        var listId = 1L;
        var problemId = 10L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2, 3 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(problemId))
            .ReturnsAsync(true);

        _problemListRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProblemList>()))
            .ReturnsAsync((ProblemList pl) => pl);

        await _service.AddProblemToListAsync(listId, problemId, userId);

        _problemListRepositoryMock.Verify(r => r.UpdateAsync(It.Is<ProblemList>(pl => pl.ProblemIds.Contains(problemId))), Times.Once);
    }

    [Fact]
    public async Task AddProblemToListAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var listId = 1L;
        var problemId = 999L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(problemId))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddProblemToListAsync(listId, problemId, userId));
    }

    [Fact]
    public async Task AddProblemToListAsync_WithDuplicateProblem_ShouldThrowException()
    {
        var listId = 1L;
        var problemId = 2L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2, 3 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        _problemRepositoryMock
            .Setup(r => r.ExistsAsync(problemId))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddProblemToListAsync(listId, problemId, userId));
    }

    [Fact]
    public async Task RemoveProblemFromListAsync_WithValidData_ShouldRemoveProblem()
    {
        var listId = 1L;
        var problemId = 2L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2, 3 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        _problemListRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProblemList>()))
            .ReturnsAsync((ProblemList pl) => pl);

        await _service.RemoveProblemFromListAsync(listId, problemId, userId);

        _problemListRepositoryMock.Verify(r => r.UpdateAsync(It.Is<ProblemList>(pl => !pl.ProblemIds.Contains(problemId))), Times.Once);
    }

    [Fact]
    public async Task RemoveProblemFromListAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var listId = 1L;
        var problemId = 999L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2, 3 },
            IsPublic = false
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RemoveProblemFromListAsync(listId, problemId, userId));
    }

    [Fact]
    public async Task ProblemListExistsAsync_WithExistingList_ShouldReturnTrue()
    {
        var listId = 1L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = 100L,
            ProblemIds = new long[] { 1, 2 },
            IsPublic = true
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        var result = await _service.ProblemListExistsAsync(listId);

        Assert.True(result);
    }

    [Fact]
    public async Task ProblemListExistsAsync_WithNonExistentList_ShouldReturnFalse()
    {
        var listId = 999L;

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync((ProblemList?)null);

        var result = await _service.ProblemListExistsAsync(listId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsOwnerAsync_WithOwner_ShouldReturnTrue()
    {
        var listId = 1L;
        var userId = 100L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = userId,
            ProblemIds = new long[] { 1, 2 },
            IsPublic = true
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        var result = await _service.IsOwnerAsync(listId, userId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsOwnerAsync_WithNonOwner_ShouldReturnFalse()
    {
        var listId = 1L;
        var ownerId = 100L;
        var differentUserId = 200L;
        var existingList = new ProblemList
        {
            Id = listId,
            Title = "Title",
            Description = "Description",
            OwnerId = ownerId,
            ProblemIds = new long[] { 1, 2 },
            IsPublic = true
        };

        _problemListRepositoryMock
            .Setup(r => r.GetByIdAsync(listId))
            .ReturnsAsync(existingList);

        var result = await _service.IsOwnerAsync(listId, differentUserId);

        Assert.False(result);
    }
}
