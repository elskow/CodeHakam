using ContentService.DTOs.Common;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/problem-lists")]
public class ProblemListsController(
    IProblemListService problemListService,
    IProblemService problemService,
    ILogger<ProblemListsController> logger)
    : BaseApiController
{
    /// <summary>
    ///     Get all public problem lists (paginated)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProblemListResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicLists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var lists = await problemListService.GetPublicListsAsync(page, pageSize);
            var totalCount = await problemListService.GetPublicListsCountAsync();

            var response = new PagedResponse<ProblemListResponse>
            {
                Items = lists.Select(MapToProblemListResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(ApiResponse<PagedResponse<ProblemListResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetPublicLists");
        }
    }

    /// <summary>
    ///     Get problem lists created by a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ProblemListResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserLists(long userId)
    {
        try
        {
            var lists = await problemListService.GetListsByOwnerAsync(userId);

            // Filter to only show public lists unless requesting own lists
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUserId = GetUserIdFromClaims();
                if (currentUserId != userId)
                {
                    lists = lists.Where(l => l.IsPublic);
                }
            }
            else
            {
                lists = lists.Where(l => l.IsPublic);
            }

            var response = lists.Select(MapToProblemListResponse).ToList();

            return Ok(ApiResponse<List<ProblemListResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetUserLists");
        }
    }

    /// <summary>
    ///     Get current user's problem lists
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<ProblemListResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyLists()
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var lists = await problemListService.GetListsByOwnerAsync(userId);
            var response = lists.Select(MapToProblemListResponse).ToList();

            return Ok(ApiResponse<List<ProblemListResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetMyLists");
        }
    }

    /// <summary>
    ///     Get a specific problem list by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProblemListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemList(long id)
    {
        try
        {
            var list = await problemListService.GetListAsync(id);
            if (list == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem list not found."));
            }

            // Check visibility
            if (!list.IsPublic)
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Forbid();
                }

                var userId = GetUserIdFromClaims();
                if (list.OwnerId != userId && !IsAdmin())
                {
                    return Forbid();
                }
            }

            return Ok(ApiResponse<ProblemListResponse>.SuccessResponse(MapToProblemListResponseWithProblems(list)));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetList");
        }
    }

    /// <summary>
    ///     Create a new problem list (requires authentication)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProblemListResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProblemList([FromBody] CreateProblemListRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Validation failed",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        try
        {
            var userId = GetUserIdFromClaims();

            // Verify all problem IDs exist
            foreach (var problemId in request.ProblemIds)
            {
                var exists = await problemService.ProblemExistsAsync(problemId);
                if (!exists)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Problem with ID {problemId} not found."));
                }
            }

            var list = await problemListService.CreateListAsync(
                request.Name,
                request.Description,
                userId,
                request.IsPublic,
                request.ProblemIds);

            logger.LogInformation(
                "Problem list created: ID {ListId}, Owner {OwnerId}, Problems {ProblemCount}",
                list.Id, userId, request.ProblemIds.Count);

            return CreatedAtAction(
                nameof(GetProblemList),
                new { id = list.Id },
                ApiResponse<ProblemListResponse>.SuccessResponse(
                    MapToProblemListResponse(list),
                    "Problem list created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "CreateList");
        }
    }

    /// <summary>
    ///     Update a problem list (requires ownership or Admin role)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProblemListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProblemList(long id, [FromBody] CreateProblemListRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Validation failed",
                ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
            ));
        }

        try
        {
            var userId = GetUserIdFromClaims();

            var existingList = await problemListService.GetListAsync(id);
            if (existingList == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem list not found."));
            }

            // Check authorization
            if (existingList.OwnerId != userId && !IsAdmin())
            {
                return Forbid();
            }

            var list = await problemListService.UpdateListAsync(
                id,
                userId,
                request.Name,
                request.Description,
                request.IsPublic);

            logger.LogInformation("Problem list {ListId} updated by user {UserId}", id, userId);

            return Ok(ApiResponse<ProblemListResponse>.SuccessResponse(
                MapToProblemListResponse(list),
                "Problem list updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UpdateList");
        }
    }

    /// <summary>
    ///     Delete a problem list (requires ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteProblemList(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var list = await problemListService.GetListAsync(id);
            if (list == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem list not found."));
            }

            // Check authorization
            if (list.OwnerId != userId && !IsAdmin())
            {
                return Forbid();
            }

            await problemListService.DeleteListAsync(id, userId);

            logger.LogInformation("Problem list {ListId} deleted by user {UserId}", id, userId);

            return Ok(ApiResponse<object>.SuccessResponse(new { id }, "Problem list deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteList");
        }
    }

    /// <summary>
    ///     Add a problem to a list (requires ownership or Admin role)
    /// </summary>
    [HttpPost("{id}/problems/{problemId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProblemListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddProblemToList(long id, long problemId)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var list = await problemListService.GetListAsync(id);
            if (list == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem list not found."));
            }

            // Check authorization
            if (list.OwnerId != userId && !IsAdmin())
            {
                return Forbid();
            }

            // Verify problem exists
            var problemExists = await problemService.ProblemExistsAsync(problemId);
            if (!problemExists)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            await problemListService.AddProblemToListAsync(id, problemId, userId);

            logger.LogInformation(
                "Problem {ProblemId} added to list {ListId} by user {UserId}",
                problemId, id, userId);

            var updatedList = await problemListService.GetListAsync(id);
            return Ok(ApiResponse<ProblemListResponse>.SuccessResponse(
                MapToProblemListResponseWithProblems(updatedList!),
                "Problem added to list successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "AddProblemToList");
        }
    }

    /// <summary>
    ///     Remove a problem from a list (requires ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}/problems/{problemId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProblemListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveProblemFromList(long id, long problemId)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var list = await problemListService.GetListAsync(id);
            if (list == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem list not found."));
            }

            // Check authorization
            if (list.OwnerId != userId && !IsAdmin())
            {
                return Forbid();
            }

            await problemListService.RemoveProblemFromListAsync(id, problemId, userId);

            logger.LogInformation(
                "Problem {ProblemId} removed from list {ListId} by user {UserId}",
                problemId, id, userId);

            var updatedList = await problemListService.GetListAsync(id);
            return Ok(ApiResponse<ProblemListResponse>.SuccessResponse(
                MapToProblemListResponseWithProblems(updatedList!),
                "Problem removed from list successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "RemoveProblemFromList");
        }
    }

    private static ProblemListResponse MapToProblemListResponse(ProblemList list)
    {
        return new ProblemListResponse
        {
            Id = list.Id,
            Name = list.Title,
            Description = list.Description,
            OwnerId = list.OwnerId,
            IsPublic = list.IsPublic,
            ProblemCount = list.ProblemIds.Length,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt,
            Problems = new List<ProblemResponse>()
        };
    }

    private static ProblemListResponse MapToProblemListResponseWithProblems(ProblemList list)
    {
        return new ProblemListResponse
        {
            Id = list.Id,
            Name = list.Title,
            Description = list.Description,
            OwnerId = list.OwnerId,
            IsPublic = list.IsPublic,
            ProblemCount = list.ProblemIds.Length,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt,
            Problems = new List<ProblemResponse>() // Note: Would need to fetch problems separately
        };
    }
}
