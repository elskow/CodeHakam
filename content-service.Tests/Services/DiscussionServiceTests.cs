namespace ContentService.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enums;
using Models;
using Repositories.Interfaces;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class DiscussionServiceTests
{
    private readonly Mock<IDiscussionRepository> _discussionRepositoryMock;
    private readonly Mock<IProblemRepository> _problemRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly DiscussionService _service;

    public DiscussionServiceTests()
    {
        _discussionRepositoryMock = new Mock<IDiscussionRepository>();
        _problemRepositoryMock = new Mock<IProblemRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        var loggerMock = new Mock<ILogger<DiscussionService>>();

        _service = new DiscussionService(
            _discussionRepositoryMock.Object,
            _problemRepositoryMock.Object,
            _eventPublisherMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task GetDiscussionAsync_WithValidId_ShouldReturnDiscussion()
    {
        var discussionId = 1L;
        var expectedDiscussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 100L,
            Title = "How to solve this?",
            Content = "I'm stuck on this problem",
            VoteCount = 5,
            CommentCount = 3
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(expectedDiscussion);

        var result = await _service.GetDiscussionAsync(discussionId, includeComments: false);

        Assert.NotNull(result);
        Assert.Equal(discussionId, result.Id);
        Assert.Equal("How to solve this?", result.Title);
    }

    [Fact]
    public async Task GetDiscussionAsync_WithIncludeComments_ShouldReturnDiscussionWithComments()
    {
        var discussionId = 1L;
        var expectedDiscussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 100L,
            Title = "How to solve this?",
            Content = "I'm stuck on this problem",
            VoteCount = 5,
            CommentCount = 3
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, true))
            .ReturnsAsync(expectedDiscussion);

        var result = await _service.GetDiscussionAsync(discussionId, includeComments: true);

        Assert.NotNull(result);
        Assert.Equal(discussionId, result.Id);
    }

    [Fact]
    public async Task GetDiscussionsAsync_WithProblemId_ShouldReturnDiscussionsForProblem()
    {
        var problemId = 10L;
        var page = 1;
        var pageSize = 10;
        var discussions = new List<Discussion>
        {
            new Discussion { Id = 1, ProblemId = problemId, UserId = 100L, Title = "Discussion 1", Content = "Content 1" },
            new Discussion { Id = 2, ProblemId = problemId, UserId = 101L, Title = "Discussion 2", Content = "Content 2" }
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId, page, pageSize))
            .ReturnsAsync(discussions);

        _discussionRepositoryMock
            .Setup(r => r.GetCountByProblemAsync(problemId))
            .ReturnsAsync(2);

        var result = await _service.GetDiscussionsAsync(problemId, page, pageSize);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Discussions.Count());
    }

    [Fact]
    public async Task GetDiscussionsAsync_WithoutProblemId_ShouldReturnAllDiscussions()
    {
        var page = 1;
        var pageSize = 10;
        var discussions = new List<Discussion>
        {
            new Discussion { Id = 1, ProblemId = 10L, UserId = 100L, Title = "Discussion 1", Content = "Content 1" },
            new Discussion { Id = 2, ProblemId = 11L, UserId = 101L, Title = "Discussion 2", Content = "Content 2" },
            new Discussion { Id = 3, ProblemId = 12L, UserId = 102L, Title = "Discussion 3", Content = "Content 3" }
        };

        _discussionRepositoryMock
            .Setup(r => r.GetAllAsync(page, pageSize))
            .ReturnsAsync(discussions);

        _discussionRepositoryMock
            .Setup(r => r.GetTotalCountAsync())
            .ReturnsAsync(3);

        var result = await _service.GetDiscussionsAsync(null, page, pageSize);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Discussions.Count());
    }

    [Fact]
    public async Task CreateDiscussionAsync_WithValidData_ShouldCreateDiscussionAndPublishEvent()
    {
        var problemId = 10L;
        var title = "Need help with this problem";
        var content = "I don't understand the approach";
        var authorId = 100L;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Medium,
            AuthorId = 200L
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        _discussionRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Discussion>()))
            .ReturnsAsync((Discussion d) =>
            {
                d.Id = 1L;
                return d;
            });

        _eventPublisherMock
            .Setup(e => e.PublishDiscussionCreatedAsync(
                It.IsAny<long>(),
                problemId,
                title,
                authorId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateDiscussionAsync(problemId, authorId, title, content);

        Assert.NotNull(result);
        Assert.Equal(problemId, result.ProblemId);
        Assert.Equal(title, result.Title);
        Assert.Equal(content, result.Content);
        Assert.Equal(authorId, result.UserId);
        Assert.Equal(0, result.VoteCount);

        _discussionRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Discussion>()), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishDiscussionCreatedAsync(It.IsAny<long>(), problemId, title, authorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateDiscussionAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var problemId = 999L;

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync((Problem?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateDiscussionAsync(problemId, 100L, "Title", "Content"));
    }

    [Fact]
    public async Task UpdateDiscussionAsync_WithValidData_ShouldUpdateDiscussion()
    {
        var discussionId = 1L;
        var userId = 100L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = userId,
            Title = "Old title",
            Content = "Old content",
            VoteCount = 5
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Discussion>()))
            .ReturnsAsync((Discussion d) => d);

        var result = await _service.UpdateDiscussionAsync(discussionId, userId, "New title", "New content");

        Assert.Equal("New title", result.Title);
        Assert.Equal("New content", result.Content);

        _discussionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Discussion>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDiscussionAsync_WithNonExistentDiscussion_ShouldThrowException()
    {
        var discussionId = 999L;

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync((Discussion?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateDiscussionAsync(discussionId, 100L, "Title", "Content"));
    }

    [Fact]
    public async Task UpdateDiscussionAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var discussionId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = authorId,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UpdateDiscussionAsync(discussionId, differentUserId, "New title", "New content"));
    }

    [Fact]
    public async Task DeleteDiscussionAsync_WithValidData_ShouldDeleteDiscussion()
    {
        var discussionId = 1L;
        var userId = 100L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = userId,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.DeleteAsync(discussionId))
            .ReturnsAsync(true);

        await _service.DeleteDiscussionAsync(discussionId, userId);

        _discussionRepositoryMock.Verify(r => r.DeleteAsync(discussionId), Times.Once);
    }

    [Fact]
    public async Task DeleteDiscussionAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var discussionId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = authorId,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.DeleteDiscussionAsync(discussionId, differentUserId));
    }

    [Fact]
    public async Task AddCommentAsync_WithValidData_ShouldAddComment()
    {
        var discussionId = 1L;
        var content = "This is a comment";
        var authorId = 100L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 200L,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.CreateCommentAsync(It.IsAny<DiscussionComment>()))
            .ReturnsAsync((DiscussionComment c) =>
            {
                c.Id = 1L;
                return c;
            });

        _discussionRepositoryMock
            .Setup(r => r.IncrementCommentCountAsync(discussionId))
            .Returns(Task.CompletedTask);

        var result = await _service.AddCommentAsync(discussionId, authorId, content);

        Assert.NotNull(result);
        Assert.Equal(discussionId, result.DiscussionId);
        Assert.Equal(content, result.Content);
        Assert.Equal(authorId, result.UserId);

        _discussionRepositoryMock.Verify(r => r.CreateCommentAsync(It.IsAny<DiscussionComment>()), Times.Once);
        _discussionRepositoryMock.Verify(r => r.IncrementCommentCountAsync(discussionId), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_WithParentComment_ShouldAddReply()
    {
        var discussionId = 1L;
        var parentCommentId = 5L;
        var content = "This is a reply";
        var authorId = 100L;

        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 200L,
            Title = "Title",
            Content = "Content"
        };

        var parentComment = new DiscussionComment
        {
            Id = parentCommentId,
            DiscussionId = discussionId,
            UserId = 300L,
            Content = "Parent comment"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(parentCommentId))
            .ReturnsAsync(parentComment);

        _discussionRepositoryMock
            .Setup(r => r.CreateCommentAsync(It.IsAny<DiscussionComment>()))
            .ReturnsAsync((DiscussionComment c) =>
            {
                c.Id = 1L;
                return c;
            });

        _discussionRepositoryMock
            .Setup(r => r.IncrementCommentCountAsync(discussionId))
            .Returns(Task.CompletedTask);

        var result = await _service.AddCommentAsync(discussionId, authorId, content, parentCommentId);

        Assert.NotNull(result);
        Assert.Equal(discussionId, result.DiscussionId);
        Assert.Equal(parentCommentId, result.ParentId);
        Assert.Equal(content, result.Content);
        Assert.Equal(authorId, result.UserId);
    }

    [Fact]
    public async Task AddCommentAsync_WithInvalidParentComment_ShouldThrowException()
    {
        var discussionId = 1L;
        var parentCommentId = 999L;
        var authorId = 100L;
        var content = "Test comment";

        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 200L,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(parentCommentId))
            .ReturnsAsync((DiscussionComment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddCommentAsync(discussionId, authorId, content, parentCommentId));
    }

    [Fact]
    public async Task UpdateCommentAsync_WithValidData_ShouldUpdateComment()
    {
        var commentId = 1L;
        var userId = 100L;
        var comment = new DiscussionComment
        {
            Id = commentId,
            DiscussionId = 10L,
            UserId = userId,
            Content = "Old content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        _discussionRepositoryMock
            .Setup(r => r.UpdateCommentAsync(It.IsAny<DiscussionComment>()))
            .ReturnsAsync((DiscussionComment c) => c);

        var result = await _service.UpdateCommentAsync(commentId, "New content", userId);

        Assert.Equal("New content", result.Content);

        _discussionRepositoryMock.Verify(r => r.UpdateCommentAsync(It.IsAny<DiscussionComment>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCommentAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var commentId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var comment = new DiscussionComment
        {
            Id = commentId,
            DiscussionId = 10L,
            UserId = authorId,
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UpdateCommentAsync(commentId, "New content", differentUserId));
    }

    [Fact]
    public async Task DeleteCommentAsync_WithValidData_ShouldDeleteComment()
    {
        var commentId = 1L;
        var userId = 100L;
        var discussionId = 10L;
        var comment = new DiscussionComment
        {
            Id = commentId,
            DiscussionId = discussionId,
            UserId = userId,
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        _discussionRepositoryMock
            .Setup(r => r.DeleteCommentAsync(commentId))
            .ReturnsAsync(true);

        _discussionRepositoryMock
            .Setup(r => r.DecrementCommentCountAsync(discussionId))
            .Returns(Task.CompletedTask);

        await _service.DeleteCommentAsync(commentId, userId);

        _discussionRepositoryMock.Verify(r => r.DeleteCommentAsync(commentId), Times.Once);
        _discussionRepositoryMock.Verify(r => r.DecrementCommentCountAsync(discussionId), Times.Once);
    }

    [Fact]
    public async Task VoteDiscussionAsync_WithUpvote_ShouldIncrementVoteCount()
    {
        var discussionId = 1L;
        var userId = 100L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 200L,
            Title = "Title",
            Content = "Content",
            VoteCount = 5
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.IncrementVoteCountAsync(discussionId, 1))
            .Returns(Task.CompletedTask);

        await _service.VoteDiscussionAsync(discussionId, upvote: true, userId);

        _discussionRepositoryMock.Verify(r => r.IncrementVoteCountAsync(discussionId, 1), Times.Once);
    }

    [Fact]
    public async Task VoteDiscussionAsync_WithDownvote_ShouldDecrementVoteCount()
    {
        var discussionId = 1L;
        var userId = 100L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = 200L,
            Title = "Title",
            Content = "Content",
            VoteCount = 5
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        _discussionRepositoryMock
            .Setup(r => r.IncrementVoteCountAsync(discussionId, -1))
            .Returns(Task.CompletedTask);

        await _service.VoteDiscussionAsync(discussionId, upvote: false, userId);

        _discussionRepositoryMock.Verify(r => r.IncrementVoteCountAsync(discussionId, -1), Times.Once);
    }

    [Fact]
    public async Task DiscussionExistsAsync_WithExistingDiscussion_ShouldReturnTrue()
    {
        var discussionId = 1L;

        _discussionRepositoryMock
            .Setup(r => r.ExistsAsync(discussionId))
            .ReturnsAsync(true);

        var result = await _service.DiscussionExistsAsync(discussionId);

        Assert.True(result);
    }

    [Fact]
    public async Task DiscussionExistsAsync_WithNonExistentDiscussion_ShouldReturnFalse()
    {
        var discussionId = 999L;

        _discussionRepositoryMock
            .Setup(r => r.ExistsAsync(discussionId))
            .ReturnsAsync(false);

        var result = await _service.DiscussionExistsAsync(discussionId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDiscussionAuthorAsync_WithAuthor_ShouldReturnTrue()
    {
        var discussionId = 1L;
        var userId = 100L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = userId,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        var result = await _service.IsDiscussionAuthorAsync(discussionId, userId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsDiscussionAuthorAsync_WithNonAuthor_ShouldReturnFalse()
    {
        var discussionId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var discussion = new Discussion
        {
            Id = discussionId,
            ProblemId = 10L,
            UserId = authorId,
            Title = "Title",
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetByIdAsync(discussionId, false))
            .ReturnsAsync(discussion);

        var result = await _service.IsDiscussionAuthorAsync(discussionId, differentUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsCommentAuthorAsync_WithAuthor_ShouldReturnTrue()
    {
        var commentId = 1L;
        var userId = 100L;
        var comment = new DiscussionComment
        {
            Id = commentId,
            DiscussionId = 10L,
            UserId = userId,
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        var result = await _service.IsCommentAuthorAsync(commentId, userId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsCommentAuthorAsync_WithNonAuthor_ShouldReturnFalse()
    {
        var commentId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;
        var comment = new DiscussionComment
        {
            Id = commentId,
            DiscussionId = 10L,
            UserId = authorId,
            Content = "Content"
        };

        _discussionRepositoryMock
            .Setup(r => r.GetCommentByIdAsync(commentId))
            .ReturnsAsync(comment);

        var result = await _service.IsCommentAuthorAsync(commentId, differentUserId);

        Assert.False(result);
    }
}
