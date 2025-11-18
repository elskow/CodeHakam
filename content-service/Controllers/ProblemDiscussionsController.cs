using ContentService.DTOs.Common;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/problems/{problemId}/discussions")]
public class ProblemDiscussionsController(
    IDiscussionService discussionService,
    IProblemService problemService,
    ILogger<ProblemDiscussionsController> logger) : BaseApiController
{
    /// <summary>
    ///     Get discussions for a specific problem
    /// </summary>
    [HttpGet]
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
    ///     Create a discussion for a problem (requires authentication)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDiscussion(long problemId, [FromBody] CreateDiscussionRequest request)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var userId = GetUserIdFromClaims();

            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var discussion = await discussionService.CreateDiscussionAsync(
                problemId,
                userId,
                request.Title,
                request.Content);

            var response = MapToDiscussionResponse(discussion);

            logger.LogInformation(
                "Discussion created: ID {DiscussionId}, Problem {ProblemId}, User {UserId}",
                discussion.Id, problemId, userId);

            return CreatedAtAction(
                nameof(GetProblemDiscussions),
                new { problemId, page = 1, pageSize = 20 },
                ApiResponse<DiscussionResponse>.SuccessResponse(
                    response,
                    "Discussion created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "CreateDiscussion");
        }
    }

    /// <summary>
    ///     Get a specific discussion by ID with comments
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscussion(long problemId, long id)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var discussion = await discussionService.GetDiscussionAsync(id, includeComments: true);
            if (discussion == null || discussion.ProblemId != problemId)
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
    ///     Update a discussion (requires ownership or Admin role)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateDiscussion(long problemId, long id, [FromBody] UpdateDiscussionRequest request)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var userId = GetUserIdFromClaims();

            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null || discussion.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            // Check authorization - only author or admin can update discussion
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteDiscussion(long problemId, long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null || discussion.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            // Check authorization - only author or admin can delete
            if (discussion.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            await discussionService.DeleteDiscussionAsync(id, userId);

            logger.LogInformation("Discussion {DiscussionId} deleted by user {UserId}", id, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteDiscussion");
        }
    }

    /// <summary>
    ///     Add a comment to a discussion (requires authentication)
    /// </summary>
    [HttpPost("{id}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddComment(long problemId, long id, [FromBody] AddCommentRequest request)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var userId = GetUserIdFromClaims();

            // Verify discussion exists and belongs to this problem
            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null || discussion.ProblemId != problemId)
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
                id,
                userId,
                request.Content,
                request.ParentId);

            logger.LogInformation(
                "Comment added: ID {CommentId}, Discussion {DiscussionId}, User {UserId}",
                comment.Id, id, userId);

            return CreatedAtAction(
                nameof(GetDiscussion),
                new { problemId, id },
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
    [HttpDelete("{id}/comments/{commentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteComment(long problemId, long id, long commentId)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var comment = await discussionService.GetCommentAsync(commentId);
            if (comment == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Comment not found."));
            }

            // Verify comment belongs to discussion
            if (comment.DiscussionId != id)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Comment does not belong to this discussion."));
            }

            // Verify discussion belongs to problem
            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null || discussion.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Discussion not found."));
            }

            // Check authorization - only author or admin can delete comment
            if (comment.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            await discussionService.DeleteCommentAsync(commentId, userId);

            logger.LogInformation("Comment {CommentId} deleted by user {UserId}", commentId, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteComment");
        }
    }

    /// <summary>
    ///     Vote on a discussion (upvote/downvote)
    /// </summary>
    [HttpPut("{id}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VoteDiscussion(long problemId, long id, [FromBody] VoteRequest request)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var userId = GetUserIdFromClaims();

            var discussion = await discussionService.GetDiscussionAsync(id);
            if (discussion == null || discussion.ProblemId != problemId)
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