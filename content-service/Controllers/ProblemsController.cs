using ContentService.Data;
using ContentService.DTOs;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProblemsController(
    IProblemService problemService,
    ContentDbContext dbContext,
    ILogger<ProblemsController> logger) : BaseApiController
{
    /// <summary>
    ///     Get paginated list of problems
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProblemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProblems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? difficulty = null,
        [FromQuery] string? visibility = null)
    {
        try
        {
            Difficulty? difficultyEnum = null;
            if (!string.IsNullOrEmpty(difficulty) && Enum.TryParse<Difficulty>(difficulty, ignoreCase: true, out var d))
            {
                difficultyEnum = d;
            }

            ProblemVisibility? visibilityEnum = null;
            if (!string.IsNullOrEmpty(visibility) && Enum.TryParse<ProblemVisibility>(visibility, ignoreCase: true, out var v))
            {
                visibilityEnum = v;
            }

            var problems = await problemService.GetProblemsAsync(page, pageSize, difficultyEnum, visibilityEnum);
            var totalCount = await problemService.GetTotalProblemsCountAsync();

            var problemsList = problems.ToList();
            var authorIds = problemsList.Select(p => p.AuthorId).Distinct().ToList();
            var authorProfiles = await dbContext.UserProfiles
                .Where(up => authorIds.Contains(up.UserId))
                .ToDictionaryAsync(up => up.UserId);

            var response = new PagedResponse<ProblemResponse>
            {
                Items = problemsList.Select(p => MapToProblemResponse(p, authorProfiles)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(ApiResponse<PagedResponse<ProblemResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblems");
        }
    }

    /// <summary>
    ///     Search problems by title, tags, or difficulty
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProblemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProblems(
        [FromQuery] string? query = null,
        [FromQuery] string? difficulty = null,
        [FromQuery] List<string>? tags = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            Difficulty? difficultyEnum = null;
            if (!string.IsNullOrEmpty(difficulty) && Enum.TryParse<Difficulty>(difficulty, ignoreCase: true, out var d))
            {
                difficultyEnum = d;
            }

            var problems = await problemService.SearchProblemsAsync(
                query,
                difficultyEnum,
                tags,
                page,
                pageSize);

            var totalCount = await problemService.GetTotalProblemsCountAsync();

            var problemsList = problems.ToList();
            var authorIds = problemsList.Select(p => p.AuthorId).Distinct().ToList();
            var authorProfiles = await dbContext.UserProfiles
                .Where(up => authorIds.Contains(up.UserId))
                .ToDictionaryAsync(up => up.UserId);

            var response = new PagedResponse<ProblemResponse>
            {
                Items = problemsList.Select(p => MapToProblemResponse(p, authorProfiles)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(ApiResponse<PagedResponse<ProblemResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "SearchProblems");
        }
    }

    /// <summary>
    ///     Get a problem by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProblemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProblem(long id)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(id);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            await problemService.IncrementViewCountAsync(id);

            var authorProfile = await dbContext.UserProfiles.FindAsync(problem.AuthorId);
            var authorProfiles = new Dictionary<long, UserProfile>();
            if (authorProfile != null)
            {
                authorProfiles[problem.AuthorId] = authorProfile;
            }

            return Ok(ApiResponse<ProblemResponse>.SuccessResponse(MapToProblemResponse(problem, authorProfiles)));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblem");
        }
    }

    /// <summary>
    ///     Get a problem by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProblemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemBySlug(string slug)
    {
        try
        {
            var problem = await problemService.GetProblemBySlugAsync(slug);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            await problemService.IncrementViewCountAsync(problem.Id);

            var authorProfile = await dbContext.UserProfiles.FindAsync(problem.AuthorId);
            var authorProfiles = new Dictionary<long, UserProfile>();
            if (authorProfile != null)
            {
                authorProfiles[problem.AuthorId] = authorProfile;
            }

            return Ok(ApiResponse<ProblemResponse>.SuccessResponse(MapToProblemResponse(problem, authorProfiles)));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemBySlug");
        }
    }

    /// <summary>
    ///     Create a new problem (requires ProblemSetter or Admin role)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProblemResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProblem([FromBody] CreateProblemRequest request)
    {
        try
        {
            if (!IsProblemSetter())
            {
                return Forbid();
            }

            var userId = GetUserIdFromClaims();

            var problem = await problemService.CreateProblemAsync(
                request.Title,
                request.Description,
                request.InputFormat,
                request.OutputFormat,
                request.Constraints,
                Enum.Parse<Difficulty>(request.Difficulty, ignoreCase: true),
                request.TimeLimit,
                request.MemoryLimit,
                userId,
                request.Tags,
                string.IsNullOrEmpty(request.Visibility)
                    ? ProblemVisibility.Public
                    : Enum.Parse<ProblemVisibility>(request.Visibility, ignoreCase: true),
                request.HintText);

            logger.LogInformation("Problem created successfully. ID: {ProblemId}, Author: {AuthorId}", problem.Id, userId);

            var authorProfile = await dbContext.UserProfiles.FindAsync(userId);
            var authorProfiles = new Dictionary<long, UserProfile>();
            if (authorProfile != null)
            {
                authorProfiles[userId] = authorProfile;
            }

            return CreatedAtAction(
                nameof(GetProblem),
                new { id = problem.Id },
                ApiResponse<ProblemResponse>.SuccessResponse(
                    MapToProblemResponse(problem, authorProfiles),
                    "Problem created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "CreateProblem");
        }
    }

    /// <summary>
    ///     Update an existing problem (requires ownership or Admin role)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProblemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProblem(long id, [FromBody] UpdateProblemRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            Difficulty? difficulty = null;
            if (!string.IsNullOrEmpty(request.Difficulty))
            {
                difficulty = Enum.Parse<Difficulty>(request.Difficulty, ignoreCase: true);
            }

            ProblemVisibility? visibility = null;
            if (!string.IsNullOrEmpty(request.Visibility))
            {
                visibility = Enum.Parse<ProblemVisibility>(request.Visibility, ignoreCase: true);
            }

            var problem = await problemService.UpdateProblemAsync(
                id,
                userId,
                request.Title,
                request.Description,
                request.InputFormat,
                request.OutputFormat,
                request.Constraints,
                difficulty,
                request.TimeLimit,
                request.MemoryLimit,
                request.Tags,
                visibility,
                request.HintText);

            logger.LogInformation("Problem updated successfully. ID: {ProblemId}, User: {UserId}", id, userId);

            var authorProfile = await dbContext.UserProfiles.FindAsync(problem.AuthorId);
            var authorProfiles = new Dictionary<long, UserProfile>();
            if (authorProfile != null)
            {
                authorProfiles[problem.AuthorId] = authorProfile;
            }

            return Ok(ApiResponse<ProblemResponse>.SuccessResponse(
                MapToProblemResponse(problem, authorProfiles),
                "Problem updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UpdateProblem");
        }
    }

    /// <summary>
    ///     Delete a problem (requires ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProblem(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            await problemService.DeleteProblemAsync(id, userId);

            logger.LogInformation("Problem deleted successfully. ID: {ProblemId}, User: {UserId}", id, userId);

            return Ok(ApiResponse<object>.SuccessResponse(new { id }, "Problem deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteProblem");
        }
    }

    private static ProblemResponse MapToProblemResponse(Problem problem, Dictionary<long, UserProfile> authorProfiles)
    {
        authorProfiles.TryGetValue(problem.AuthorId, out var authorProfile);

        return new ProblemResponse
        {
            Id = problem.Id,
            Title = problem.Title,
            Slug = problem.Slug,
            Description = problem.Description,
            InputFormat = problem.InputFormat,
            OutputFormat = problem.OutputFormat,
            Constraints = problem.Constraints,
            Difficulty = problem.Difficulty.ToString(),
            TimeLimit = problem.TimeLimit,
            MemoryLimit = problem.MemoryLimit,
            AuthorId = problem.AuthorId,
            AuthorName = authorProfile?.DisplayName,
            AuthorAvatar = authorProfile?.AvatarUrl,
            Visibility = problem.Visibility.ToString(),
            HintText = problem.HintText,
            Tags = problem.Tags.Select(t => t.Tag).ToList(),
            ViewCount = problem.ViewCount,
            SubmissionCount = problem.SubmissionCount,
            AcceptedCount = problem.AcceptedCount,
            AcceptanceRate = problem.SubmissionCount > 0
                ? Math.Round((double)problem.AcceptedCount / problem.SubmissionCount * 100, digits: 2)
                : 0.0,
            CreatedAt = problem.CreatedAt,
            UpdatedAt = problem.UpdatedAt
        };
    }
}
