using ContentService.Data;
using ContentService.DTOs.Common;
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
    IDiscussionService discussionService,
    IEditorialService editorialService,
    ITestCaseService testCaseService,
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

            var totalCount = await problemService.GetSearchCountAsync(query, difficultyEnum, tags);

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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProblem([FromBody] CreateProblemRequest request)
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProblem(long id, [FromBody] UpdateProblemRequest request)
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    ///     Get discussions for a specific problem
    /// </summary>
    [HttpGet("{problemId}/discussions")]
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
                Items = discussions.Select(d => new DiscussionResponse
                {
                    Id = d.Id,
                    ProblemId = d.ProblemId,
                    UserId = d.UserId,
                    Title = d.Title,
                    Content = d.Content,
                    VoteCount = d.VoteCount,
                    CommentCount = d.CommentCount,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    Comments = new List<CommentResponse>()
                }).ToList(),
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
    [HttpPost("{problemId}/discussions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DiscussionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDiscussion(long problemId, [FromBody] CreateDiscussionRequest request)
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

            var response = new DiscussionResponse
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

            logger.LogInformation(
                "Discussion created: ID {DiscussionId}, Problem {ProblemId}, User {UserId}",
                discussion.Id, problemId, userId);

            return StatusCode(StatusCodes.Status201Created,
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
    ///     Get editorial for a problem
    /// </summary>
    [HttpGet("{problemId}/editorials")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemEditorial(long problemId)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var editorial = await editorialService.GetEditorialAsync(problemId);
            if (editorial == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found for this problem."));
            }

            if (!editorial.IsPublished)
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found for this problem."));
                }

                var userId = GetUserIdFromClaims();
                var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());

                if (!isAuthorOrAdmin)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found for this problem."));
                }
            }

            var editorialResponse = new EditorialResponse
            {
                Id = editorial.Id,
                ProblemId = editorial.ProblemId,
                AuthorId = editorial.AuthorId,
                Content = editorial.Content,
                TimeComplexity = editorial.TimeComplexity,
                SpaceComplexity = editorial.SpaceComplexity,
                VideoUrl = editorial.VideoUrl,
                IsPublished = editorial.IsPublished,
                CreatedAt = editorial.CreatedAt,
                UpdatedAt = editorial.UpdatedAt
            };

            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(editorialResponse));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemEditorial");
        }
    }

    /// <summary>
    ///     Create an editorial for a problem (requires problem ownership or Admin role)
    /// </summary>
    [HttpPost("{problemId}/editorials")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEditorial(long problemId, [FromBody] CreateEditorialRequest request)
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

            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            var editorial = await editorialService.CreateOrUpdateEditorialAsync(
                problemId,
                userId,
                request.Content,
                request.TimeComplexity,
                request.SpaceComplexity,
                request.VideoUrl);

            var response = new EditorialResponse
            {
                Id = editorial.Id,
                ProblemId = editorial.ProblemId,
                AuthorId = editorial.AuthorId,
                Content = editorial.Content,
                TimeComplexity = editorial.TimeComplexity,
                SpaceComplexity = editorial.SpaceComplexity,
                VideoUrl = editorial.VideoUrl,
                IsPublished = editorial.IsPublished,
                CreatedAt = editorial.CreatedAt,
                UpdatedAt = editorial.UpdatedAt
            };

            logger.LogInformation(
                "Editorial created/updated for problem {ProblemId} by user {UserId}",
                problemId, userId);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<EditorialResponse>.SuccessResponse(
                    response,
                    "Editorial created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "CreateEditorial");
        }
    }

    /// <summary>
    ///     Get test cases for a problem (sample test cases only for non-authors)
    /// </summary>
    [HttpGet("{problemId}/testcases")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<TestCaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemTestCases(long problemId, [FromQuery] bool samplesOnly = true)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var requestSamplesOnly = samplesOnly;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserIdFromClaims();
                var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());

                if (isAuthorOrAdmin)
                {
                    requestSamplesOnly = false;
                }
            }

            var testCases = await testCaseService.GetTestCasesAsync(problemId, requestSamplesOnly);

            var response = testCases.Select(tc => new TestCaseResponse
            {
                Id = tc.Id,
                ProblemId = tc.ProblemId,
                TestNumber = tc.TestNumber,
                IsSample = tc.IsSample,
                InputFileUrl = tc.InputFileUrl,
                OutputFileUrl = tc.OutputFileUrl,
                CreatedAt = tc.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<TestCaseResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemTestCases");
        }
    }

    /// <summary>
    ///     Get test case count for a problem
    /// </summary>
    [HttpGet("{problemId}/testcases/count")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemTestCaseCount(long problemId)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var count = await testCaseService.GetTestCaseCountAsync(problemId);
            var sampleCount = await testCaseService.GetSampleTestCaseCountAsync(problemId);

            return Ok(new
            {
                problemId,
                totalCount = count,
                sampleCount,
                hiddenCount = count - sampleCount
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemTestCaseCount");
        }
    }

    /// <summary>
    ///     Upload a test case for a problem (requires problem ownership or Admin role)
    /// </summary>
    [HttpPost("{problemId}/testcases")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TestCaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadTestCase(long problemId, [FromForm] UploadTestCaseRequest request)
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

            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            var testCase = await testCaseService.UploadTestCaseAsync(
                problemId,
                request.InputFile,
                request.OutputFile,
                request.IsSample,
                request.TestNumber,
                userId);

            var response = new TestCaseResponse
            {
                Id = testCase.Id,
                ProblemId = testCase.ProblemId,
                TestNumber = testCase.TestNumber,
                IsSample = testCase.IsSample,
                InputFileUrl = testCase.InputFileUrl,
                OutputFileUrl = testCase.OutputFileUrl,
                CreatedAt = testCase.CreatedAt
            };

            logger.LogInformation(
                "Test case uploaded successfully. ID: {TestCaseId}, Problem: {ProblemId}, User: {UserId}",
                testCase.Id, problemId, userId);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<TestCaseResponse>.SuccessResponse(
                    response,
                    "Test case uploaded successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UploadTestCase");
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
