using ContentService.Data;
using ContentService.Repositories.Implementations;
using ContentService.Tests.Helpers;
using FluentAssertions;

namespace ContentService.Tests.Unit.Repositories;

public class DiscussionRepositoryTests : IDisposable
{
    private readonly ContentDbContext _context;
    private readonly DiscussionRepository _repository;

    public DiscussionRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"DiscussionRepositoryTests_{Guid.NewGuid()}");
        _repository = new DiscussionRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnDiscussion()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(discussion.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(discussion.Id);
        result.Title.Should().Be(discussion.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeComments_ShouldIncludeComments()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        await _context.DiscussionComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(discussion.Id, includeComments: true);

        result.Should().NotBeNull();
        result!.Comments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByProblemIdAsync_ShouldReturnDiscussionsForProblem()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var discussion1 = TestDataBuilder.CreateDiscussion(problemId: problem.Id, title: "Discussion 1");
        var discussion2 = TestDataBuilder.CreateDiscussion(problemId: problem.Id, title: "Discussion 2");
        var otherDiscussion = TestDataBuilder.CreateDiscussion(problemId: null, title: "General Discussion");

        await _context.Discussions.AddRangeAsync(discussion1, discussion2, otherDiscussion);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByProblemIdAsync(problem.Id, page: 1, pageSize: 10);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.ProblemId == problem.Id);
    }

    [Fact]
    public async Task GetByProblemIdAsync_ShouldOrderByPinnedThenCreatedAt()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var normalDiscussion = TestDataBuilder.CreateDiscussion(problemId: problem.Id, title: "Normal", isPinned: false);
        await _context.Discussions.AddAsync(normalDiscussion);
        await _context.SaveChangesAsync();

        await Task.Delay(10);

        var pinnedDiscussion = TestDataBuilder.CreateDiscussion(problemId: problem.Id, title: "Pinned", isPinned: true);
        await _context.Discussions.AddAsync(pinnedDiscussion);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByProblemIdAsync(problem.Id, page: 1, pageSize: 10);

        result.First().IsPinned.Should().BeTrue();
        result.First().Title.Should().Be("Pinned");
    }

    [Fact]
    public async Task GetByProblemIdAsync_WithPagination_ShouldReturnCorrectPage()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        for (var i = 1; i <= 5; i++)
        {
            var discussion = TestDataBuilder.CreateDiscussion(problemId: problem.Id, title: $"Discussion {i}");
            await _context.Discussions.AddAsync(discussion);
        }
        await _context.SaveChangesAsync();

        var page1 = await _repository.GetByProblemIdAsync(problem.Id, page: 1, pageSize: 2);
        var page2 = await _repository.GetByProblemIdAsync(problem.Id, page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDiscussions()
    {
        var discussion1 = TestDataBuilder.CreateDiscussion(title: "Discussion 1");
        var discussion2 = TestDataBuilder.CreateDiscussion(title: "Discussion 2");

        await _context.Discussions.AddRangeAsync(discussion1, discussion2);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(page: 1, pageSize: 10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            var discussion = TestDataBuilder.CreateDiscussion(title: $"Discussion {i}");
            await _context.Discussions.AddAsync(discussion);
        }
        await _context.SaveChangesAsync();

        var page1 = await _repository.GetAllAsync(page: 1, pageSize: 2);
        var page2 = await _repository.GetAllAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnCorrectCount()
    {
        var discussion1 = TestDataBuilder.CreateDiscussion(title: "Discussion 1");
        var discussion2 = TestDataBuilder.CreateDiscussion(title: "Discussion 2");

        await _context.Discussions.AddRangeAsync(discussion1, discussion2);
        await _context.SaveChangesAsync();

        var count = await _repository.GetTotalCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetCountByProblemAsync_ShouldReturnCorrectCount()
    {
        var problem = TestDataBuilder.CreateProblem();
        await _context.Problems.AddAsync(problem);
        await _context.SaveChangesAsync();

        var discussion1 = TestDataBuilder.CreateDiscussion(problemId: problem.Id);
        var discussion2 = TestDataBuilder.CreateDiscussion(problemId: problem.Id);
        var otherDiscussion = TestDataBuilder.CreateDiscussion(problemId: null);

        await _context.Discussions.AddRangeAsync(discussion1, discussion2, otherDiscussion);
        await _context.SaveChangesAsync();

        var count = await _repository.GetCountByProblemAsync(problem.Id);

        count.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDiscussion_ShouldCreateSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion();

        var result = await _repository.CreateAsync(discussion);

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be(discussion.Title);

        var savedDiscussion = await _context.Discussions.FindAsync(result.Id);
        savedDiscussion.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingDiscussion_ShouldUpdateSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        discussion.Title = "Updated Title";
        var result = await _repository.UpdateAsync(discussion);

        result.Title.Should().Be("Updated Title");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingDiscussion_ShouldDeleteSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(discussion.Id);

        result.Should().BeTrue();

        var deletedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        deletedDiscussion.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingDiscussion_ShouldReturnFalse()
    {
        var result = await _repository.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingDiscussion_ShouldReturnTrue()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(discussion.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingDiscussion_ShouldReturnFalse()
    {
        var result = await _repository.ExistsAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementVoteCountAsync_ShouldIncreaseVoteCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        await _repository.IncrementVoteCountAsync(discussion.Id, increment: 1);

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.VoteCount.Should().Be(1);
    }

    [Fact]
    public async Task IncrementVoteCountAsync_WithNegativeIncrement_ShouldDecreaseVoteCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        discussion.VoteCount = 5;
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        await _repository.IncrementVoteCountAsync(discussion.Id, increment: -2);

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.VoteCount.Should().Be(3);
    }

    [Fact]
    public async Task IncrementCommentCountAsync_ShouldIncreaseCommentCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        await _repository.IncrementCommentCountAsync(discussion.Id);

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.CommentCount.Should().Be(1);
    }

    [Fact]
    public async Task DecrementCommentCountAsync_ShouldDecreaseCommentCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        discussion.CommentCount = 5;
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        await _repository.DecrementCommentCountAsync(discussion.Id);

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.CommentCount.Should().Be(4);
    }

    [Fact]
    public async Task DecrementCommentCountAsync_WhenZero_ShouldNotGoNegative()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        discussion.CommentCount = 0;
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        await _repository.DecrementCommentCountAsync(discussion.Id);

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.CommentCount.Should().Be(0);
    }

    [Fact]
    public async Task LockAsync_WithExistingDiscussion_ShouldLockSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion(isLocked: false);
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.LockAsync(discussion.Id);

        result.Should().BeTrue();

        var lockedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        lockedDiscussion!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task LockAsync_WithNonExistingDiscussion_ShouldReturnFalse()
    {
        var result = await _repository.LockAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockAsync_WithLockedDiscussion_ShouldUnlockSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion(isLocked: true);
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.UnlockAsync(discussion.Id);

        result.Should().BeTrue();

        var unlockedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        unlockedDiscussion!.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockAsync_WithNonExistingDiscussion_ShouldReturnFalse()
    {
        var result = await _repository.UnlockAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task PinAsync_WithExistingDiscussion_ShouldPinSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion(isPinned: false);
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.PinAsync(discussion.Id);

        result.Should().BeTrue();

        var pinnedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        pinnedDiscussion!.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinAsync_WithNonExistingDiscussion_ShouldReturnFalse()
    {
        var result = await _repository.PinAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinAsync_WithPinnedDiscussion_ShouldUnpinSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion(isPinned: true);
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var result = await _repository.UnpinAsync(discussion.Id);

        result.Should().BeTrue();

        var unpinnedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        unpinnedDiscussion!.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinAsync_WithNonExistingDiscussion_ShouldReturnFalse()
    {
        var result = await _repository.UnpinAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCommentByIdAsync_WithExistingComment_ShouldReturnComment()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        await _context.DiscussionComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        var result = await _repository.GetCommentByIdAsync(comment.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(comment.Id);
    }

    [Fact]
    public async Task GetCommentByIdAsync_WithNonExistingComment_ShouldReturnNull()
    {
        var result = await _repository.GetCommentByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCommentsByDiscussionIdAsync_ShouldReturnAllComments()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment1 = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id, content: "Comment 1");
        var comment2 = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id, content: "Comment 2");

        await _context.DiscussionComments.AddRangeAsync(comment1, comment2);
        await _context.SaveChangesAsync();

        var result = await _repository.GetCommentsByDiscussionIdAsync(discussion.Id);

        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(c => c.CreatedAt);
    }

    [Fact]
    public async Task CreateCommentAsync_WithValidComment_ShouldCreateAndIncrementCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var initialCommentCount = discussion.CommentCount;

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        var result = await _repository.CreateCommentAsync(comment);

        result.Id.Should().BeGreaterThan(0);

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.CommentCount.Should().Be(initialCommentCount + 1);
    }

    [Fact]
    public async Task UpdateCommentAsync_WithExistingComment_ShouldUpdateSuccessfully()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        await _context.DiscussionComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        comment.Content = "Updated content";
        var result = await _repository.UpdateCommentAsync(comment);

        result.Content.Should().Be("Updated content");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteCommentAsync_WithExistingComment_ShouldDeleteAndDecrementCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        discussion.CommentCount = 5;
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        await _context.DiscussionComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteCommentAsync(comment.Id);

        result.Should().BeTrue();

        var deletedComment = await _context.DiscussionComments.FindAsync(comment.Id);
        deletedComment.Should().BeNull();

        var updatedDiscussion = await _context.Discussions.FindAsync(discussion.Id);
        updatedDiscussion!.CommentCount.Should().Be(4);
    }

    [Fact]
    public async Task DeleteCommentAsync_WithNonExistingComment_ShouldReturnFalse()
    {
        var result = await _repository.DeleteCommentAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementCommentVoteCountAsync_ShouldIncreaseVoteCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        await _context.DiscussionComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        await _repository.IncrementCommentVoteCountAsync(comment.Id, increment: 1);

        var updatedComment = await _context.DiscussionComments.FindAsync(comment.Id);
        updatedComment!.VoteCount.Should().Be(1);
    }

    [Fact]
    public async Task IncrementCommentVoteCountAsync_WithNegativeIncrement_ShouldDecreaseVoteCount()
    {
        var discussion = TestDataBuilder.CreateDiscussion();
        await _context.Discussions.AddAsync(discussion);
        await _context.SaveChangesAsync();

        var comment = TestDataBuilder.CreateDiscussionComment(discussionId: discussion.Id);
        comment.VoteCount = 10;
        await _context.DiscussionComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        await _repository.IncrementCommentVoteCountAsync(comment.Id, increment: -3);

        var updatedComment = await _context.DiscussionComments.FindAsync(comment.Id);
        updatedComment!.VoteCount.Should().Be(7);
    }
}
