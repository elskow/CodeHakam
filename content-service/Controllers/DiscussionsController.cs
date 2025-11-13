using ContentService.DTOs;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscussionsController(
    IDiscussionService discussionService,
    IProblemService problemService,
    ILogger<DiscussionsController> logger)
    : BaseApiController
{
    /// <summary>
    ///     Get all discussions (paginated)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DiscussionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiscussions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var discussions = await discussionService.GetDiscussionsAsync(page, pageSize);
            var totalCount = await discussionService.GetTotalDiscussionsCountAsync();

            var response = new PagedResponse<DiscussionResponse>
            {
                Items = discussions.Select(MapToDiscussionResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(ApiResponse<PagedResponse<DiscussionResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetDiscussions");
        }
    }

    /// <summary>
    ///     Get discussions for a specific problem
    /// </summary>
    [HttpGet("problem/{problemId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DiscussionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemDiscussions(
        long problemId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var discussions = await discussionService.GetDiscussionsByProblemAsync(problemId, page, pageSize);
            var totalCount = await discussionService.GetProblemDiscussionsCountAsync(problemId);

            var response = new PagedResponse<DiscussionResponse>
            {
                Items = discussions.Select(MapToDiscussionResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(ApiResponse<PagedResponse<DiscussionResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemDiscussions");
        }
    }

    /// <summary>
    ///     Get a specific discussion by ID with comments
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscussion(long id)
    {
        try
        {
            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            return Ok(ApiResponse<DiscussionResponse>.SuccessResponse(MapToDiscussionResponseWithComments(discussion)));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetDiscussion");
        }
    }

    /// <summary>
    ///     Create a new discussion (requires authentication)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDiscussion([FromBody] CreateDiscussionRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Verify problem exists
            var problem = await problemService.GetProblemAsync(request.ProblemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var discussion = await discussionService.CreateDiscussionAsync(
                request.ProblemId,
                userId,
                request.Title,
                request.Content);

            logger.LogInformation(
                "Discussion created: ID {DiscussionId}, Problem {ProblemId}, User {UserId}",
                discussion.Id, request.ProblemId, userId);

            return CreatedAtAction(
                nameof(GetDiscussion),
                new { id = discussion.Id },
                ApiResponse<DiscussionResponse>.SuccessResponse(
                    MapToDiscussionResponse(discussion),
                    "Discussion created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "CreateDiscussion");
        }
    }

    /// <summary>
    ///     Update a discussion (requires ownership or Admin role)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDiscussion(long id, [FromBody] CreateDiscussionRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            // Check authorization
            if (discussion.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            var updatedDiscussion = await discussionService.UpdateDiscussionAsync(
                id,
                userId,
                request.Title,
                request.Content);

            logger.LogInformation("Discussion {DiscussionId} updated by user {UserId}", id, userId);

            return Ok(ApiResponse<DiscussionResponse>.SuccessResponse(
                MapToDiscussionResponse(updatedDiscussion),
                "Discussion updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UpdateDiscussion");
        }
    }

    /// <summary>
    ///     Delete a discussion (requires ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDiscussion(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            // Check authorization
            if (discussion.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            await discussionService.DeleteDiscussionAsync(id, userId);

            logger.LogInformation("Discussion {DiscussionId} deleted by user {UserId}", id, userId);

            return Ok(ApiResponse<object>.SuccessResponse(new { id }, "Discussion deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteDiscussion");
        }
    }

    /// <summary>
    ///     Add a comment to a discussion (requires authentication)
    /// </summary>
    [HttpPost("comments")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Verify discussion exists
            var discussion = await discussionService.GetDiscussionAsync(request.DiscussionId);
            if (discussion == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            // If replying to a comment, verify parent exists
            if (request.ParentId.HasValue)
            {
                var parentComment = await discussionService.GetCommentAsync(request.ParentId.Value);
                if (parentComment == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Parent comment not found."));
                }
            }

            var comment = await discussionService.AddCommentAsync(
                request.DiscussionId,
                userId,
                request.Content,
                request.ParentId);

            logger.LogInformation(
                "Comment added: ID {CommentId}, Discussion {DiscussionId}, User {UserId}",
                comment.Id, request.DiscussionId, userId);

            return CreatedAtAction(
                nameof(GetDiscussion),
                new { id = request.DiscussionId },
                ApiResponse<CommentResponse>.SuccessResponse(
                    MapToCommentResponse(comment),
                    "Comment added successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "AddComment");
        }
    }

    /// <summary>
    ///     Delete a comment (requires ownership or Admin role)
    /// </summary>
    [HttpDelete("comments/{commentId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(long commentId)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var comment = await discussionService.GetCommentAsync(commentId);
            if (comment == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Comment not found."));
            }

            // Check authorization
            if (comment.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            await discussionService.DeleteCommentAsync(commentId, userId);

            logger.LogInformation("Comment {CommentId} deleted by user {UserId}", commentId, userId);

            return Ok(ApiResponse<object>.SuccessResponse(new { commentId }, "Comment deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteComment");
        }
    }

    /// <summary>
    ///     Vote on a discussion (upvote/downvote)
    /// </summary>
    [HttpPost("{id}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteDiscussion(long id, [FromBody] VoteRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            await discussionService.VoteDiscussionAsync(id, request.IsUpvote, userId);

            logger.LogInformation(
                "User {UserId} voted on discussion {DiscussionId} (upvote: {IsUpvote})",
                userId, id, request.IsUpvote);

            var updatedDiscussion = await discussionService.GetDiscussionAsync(id);
            return Ok(ApiResponse<DiscussionResponse>.SuccessResponse(
                MapToDiscussionResponse(updatedDiscussion!),
                "Vote recorded successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "VoteDiscussion");
        }
    }

    private static DiscussionResponse MapToDiscussionResponse(Discussion discussion)
    {
        return new DiscussionResponse
        {
            Id = discussion.Id,
            ProblemId = discussion.ProblemId,
            UserId = discussion.UserId,
            Title = discussion.Title,
            Content = discussion.Content,
            VoteCount = discussion.VoteCount,
            CommentCount = discussion.CommentCount,
            CreatedAt = discussion.CreatedAt,
            UpdatedAt = discussion.UpdatedAt,
            Comments = new List<CommentResponse>()
        };
    }

    private static DiscussionResponse MapToDiscussionResponseWithComments(Discussion discussion)
    {
        return new DiscussionResponse
        {
            Id = discussion.Id,
            ProblemId = discussion.ProblemId,
            UserId = discussion.UserId,
            Title = discussion.Title,
            Content = discussion.Content,
            VoteCount = discussion.VoteCount,
            CommentCount = discussion.CommentCount,
            CreatedAt = discussion.CreatedAt,
            UpdatedAt = discussion.UpdatedAt,
            Comments = discussion.Comments.Select(MapToCommentResponse).ToList()
        };
    }

    private static CommentResponse MapToCommentResponse(DiscussionComment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            DiscussionId = comment.DiscussionId,
            UserId = comment.UserId,
            ParentId = comment.ParentId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }
}

public class VoteRequest
{
    public bool IsUpvote { get; set; }
}
