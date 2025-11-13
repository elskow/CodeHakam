using ContentService.Data;
using ContentService.Repositories.Implementations;
using ContentService.Tests.Helpers;
using FluentAssertions;

namespace ContentService.Tests.Unit.Repositories;

public class ProblemListRepositoryTests : IDisposable
{
    private readonly ContentDbContext _context;
    private readonly ProblemListRepository _repository;

    public ProblemListRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"ProblemListRepositoryTests_{Guid.NewGuid()}");
        _repository = new ProblemListRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnProblemList()
    {
        var problemList = TestDataBuilder.CreateProblemList();
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(problemList.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(problemList.Id);
        result.Title.Should().Be(problemList.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOwnerAsync_ShouldReturnListsForOwner()
    {
        var list1 = TestDataBuilder.CreateProblemList(ownerId: 1, title: "Owner 1 List 1");
        var list2 = TestDataBuilder.CreateProblemList(ownerId: 1, title: "Owner 1 List 2");
        var list3 = TestDataBuilder.CreateProblemList(ownerId: 2, title: "Owner 2 List 1");

        await _context.ProblemLists.AddRangeAsync(list1, list2, list3);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByOwnerAsync(ownerId: 1, page: 1, pageSize: 10);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(pl => pl.OwnerId == 1);
    }

    [Fact]
    public async Task GetByOwnerAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            var list = TestDataBuilder.CreateProblemList(ownerId: 1, title: $"List {i}");
            await _context.ProblemLists.AddAsync(list);
        }
        await _context.SaveChangesAsync();

        var page1 = await _repository.GetByOwnerAsync(ownerId: 1, page: 1, pageSize: 2);
        var page2 = await _repository.GetByOwnerAsync(ownerId: 1, page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPublicListsAsync_ShouldReturnOnlyPublicLists()
    {
        var publicList1 = TestDataBuilder.CreateProblemList(title: "Public 1", isPublic: true);
        var publicList2 = TestDataBuilder.CreateProblemList(title: "Public 2", isPublic: true);
        var privateList = TestDataBuilder.CreateProblemList(title: "Private", isPublic: false);

        await _context.ProblemLists.AddRangeAsync(publicList1, publicList2, privateList);
        await _context.SaveChangesAsync();

        var result = await _repository.GetPublicListsAsync(page: 1, pageSize: 10);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(pl => pl.IsPublic);
    }

    [Fact]
    public async Task GetPublicListsAsync_ShouldOrderByViewCountThenCreatedAt()
    {
        var lowViewList = TestDataBuilder.CreateProblemList(title: "Low Views", isPublic: true);
        lowViewList.ViewCount = 10;
        await _context.ProblemLists.AddAsync(lowViewList);
        await _context.SaveChangesAsync();

        await Task.Delay(10);

        var highViewList = TestDataBuilder.CreateProblemList(title: "High Views", isPublic: true);
        highViewList.ViewCount = 100;
        await _context.ProblemLists.AddAsync(highViewList);
        await _context.SaveChangesAsync();

        var result = await _repository.GetPublicListsAsync(page: 1, pageSize: 10);

        result.First().ViewCount.Should().Be(100);
        result.First().Title.Should().Be("High Views");
    }

    [Fact]
    public async Task GetPublicListsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            var list = TestDataBuilder.CreateProblemList(title: $"Public List {i}", isPublic: true);
            await _context.ProblemLists.AddAsync(list);
        }
        await _context.SaveChangesAsync();

        var page1 = await _repository.GetPublicListsAsync(page: 1, pageSize: 2);
        var page2 = await _repository.GetPublicListsAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTotalPublicCountAsync_ShouldReturnCorrectCount()
    {
        var publicList1 = TestDataBuilder.CreateProblemList(isPublic: true);
        var publicList2 = TestDataBuilder.CreateProblemList(isPublic: true);
        var privateList = TestDataBuilder.CreateProblemList(isPublic: false);

        await _context.ProblemLists.AddRangeAsync(publicList1, publicList2, privateList);
        await _context.SaveChangesAsync();

        var count = await _repository.GetTotalPublicCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetCountByOwnerAsync_ShouldReturnCorrectCount()
    {
        var list1 = TestDataBuilder.CreateProblemList(ownerId: 1);
        var list2 = TestDataBuilder.CreateProblemList(ownerId: 1);
        var list3 = TestDataBuilder.CreateProblemList(ownerId: 2);

        await _context.ProblemLists.AddRangeAsync(list1, list2, list3);
        await _context.SaveChangesAsync();

        var count = await _repository.GetCountByOwnerAsync(ownerId: 1);

        count.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidProblemList_ShouldCreateSuccessfully()
    {
        var problemList = TestDataBuilder.CreateProblemList();

        var result = await _repository.CreateAsync(problemList);

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be(problemList.Title);

        var savedList = await _context.ProblemLists.FindAsync(result.Id);
        savedList.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingProblemList_ShouldUpdateSuccessfully()
    {
        var problemList = TestDataBuilder.CreateProblemList();
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        problemList.Title = "Updated Title";
        var result = await _repository.UpdateAsync(problemList);

        result.Title.Should().Be("Updated Title");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProblemList_ShouldDeleteSuccessfully()
    {
        var problemList = TestDataBuilder.CreateProblemList();
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(problemList.Id);

        result.Should().BeTrue();

        var deletedList = await _context.ProblemLists.FindAsync(problemList.Id);
        deletedList.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingProblemList_ShouldReturnFalse()
    {
        var result = await _repository.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProblemList_ShouldReturnTrue()
    {
        var problemList = TestDataBuilder.CreateProblemList();
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(problemList.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingProblemList_ShouldReturnFalse()
    {
        var result = await _repository.ExistsAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementViewCountAsync_ShouldIncreaseViewCount()
    {
        var problemList = TestDataBuilder.CreateProblemList();
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var initialViewCount = problemList.ViewCount;

        await _repository.IncrementViewCountAsync(problemList.Id);

        var updatedList = await _context.ProblemLists.FindAsync(problemList.Id);
        updatedList!.ViewCount.Should().Be(initialViewCount + 1);
    }

    [Fact]
    public async Task AddProblemAsync_WithValidProblemId_ShouldAddSuccessfully()
    {
        var problemList = TestDataBuilder.CreateProblemList(problemIds: new long[] { 1, 2, 3 });
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.AddProblemAsync(problemList.Id, problemId: 4);

        result.Should().BeTrue();

        var updatedList = await _context.ProblemLists.FindAsync(problemList.Id);
        updatedList!.ProblemIds.Should().Contain(4);
        updatedList.ProblemIds.Should().HaveCount(4);
    }

    [Fact]
    public async Task AddProblemAsync_WithDuplicateProblemId_ShouldReturnFalse()
    {
        var problemList = TestDataBuilder.CreateProblemList(problemIds: new long[] { 1, 2, 3 });
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.AddProblemAsync(problemList.Id, problemId: 2);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddProblemAsync_WithNonExistingList_ShouldReturnFalse()
    {
        var result = await _repository.AddProblemAsync(listId: 999, problemId: 1);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveProblemAsync_WithExistingProblemId_ShouldRemoveSuccessfully()
    {
        var problemList = TestDataBuilder.CreateProblemList(problemIds: new long[] { 1, 2, 3 });
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.RemoveProblemAsync(problemList.Id, problemId: 2);

        result.Should().BeTrue();

        var updatedList = await _context.ProblemLists.FindAsync(problemList.Id);
        updatedList!.ProblemIds.Should().NotContain(2);
        updatedList.ProblemIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveProblemAsync_WithNonExistingProblemId_ShouldReturnFalse()
    {
        var problemList = TestDataBuilder.CreateProblemList(problemIds: new long[] { 1, 2, 3 });
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.RemoveProblemAsync(problemList.Id, problemId: 99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveProblemAsync_WithNonExistingList_ShouldReturnFalse()
    {
        var result = await _repository.RemoveProblemAsync(listId: 999, problemId: 1);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ContainsProblemAsync_WithExistingProblem_ShouldReturnTrue()
    {
        var problemList = TestDataBuilder.CreateProblemList(problemIds: new long[] { 1, 2, 3 });
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.ContainsProblemAsync(problemList.Id, problemId: 2);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ContainsProblemAsync_WithNonExistingProblem_ShouldReturnFalse()
    {
        var problemList = TestDataBuilder.CreateProblemList(problemIds: new long[] { 1, 2, 3 });
        await _context.ProblemLists.AddAsync(problemList);
        await _context.SaveChangesAsync();

        var result = await _repository.ContainsProblemAsync(problemList.Id, problemId: 99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ContainsProblemAsync_WithNonExistingList_ShouldReturnFalse()
    {
        var result = await _repository.ContainsProblemAsync(listId: 999, problemId: 1);

        result.Should().BeFalse();
    }
}
