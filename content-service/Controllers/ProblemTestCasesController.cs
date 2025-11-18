using ContentService.DTOs.Common;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Services.Interfaces;
using ContentService.Mappers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/problems/{problemId}/test-cases")]
public class ProblemTestCasesController(
    ITestCaseService testCaseService,
    IProblemService problemService,
    ITestCaseMapper testCaseMapper,
    ILogger<ProblemTestCasesController> logger) : BaseApiController
{
/// <summary>
    ///     Get test cases for a problem (with optional count-only mode)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<TestCaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TestCaseCountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblemTestCases(long problemId, [FromQuery] bool countOnly = false)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            if (countOnly)
            {
                var count = await testCaseService.GetTestCaseCountAsync(problemId);
                var sampleCount = await testCaseService.GetSampleTestCaseCountAsync(problemId);

                var response = new TestCaseCountResponse
                {
                    ProblemId = problemId,
                    TotalCount = count,
                    SampleCount = sampleCount,
                    HiddenCount = count - sampleCount
                };

                return Ok(ApiResponse<TestCaseCountResponse>.SuccessResponse(response));
            }
            else
            {
                var samplesOnly = true; // Default to sample test cases for non-authors
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = GetUserIdFromClaims();
                    var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());
                    if (isAuthorOrAdmin)
                    {
                        samplesOnly = false;
                    }
                }

                var testCases = await testCaseService.GetTestCasesAsync(problemId, samplesOnly);
                var response = testCaseMapper.ToResponses(testCases);

                return Ok(ApiResponse<List<TestCaseResponse>>.SuccessResponse(response));
            }
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemTestCases");
        }
    }

    /// <summary>
    ///     Upload a test case for a problem (requires problem ownership or Admin role)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TestCaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadTestCase(long problemId, [FromForm] UploadTestCaseRequest request)
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

            var response = testCaseMapper.ToResponse(testCase);

            logger.LogInformation(
                "Test case uploaded successfully. ID: {TestCaseId}, Problem: {ProblemId}, User: {UserId}",
                testCase.Id, problemId, userId);

            return CreatedAtAction(
                nameof(GetProblemTestCases),
                new { problemId, countOnly = false },
                ApiResponse<TestCaseResponse>.SuccessResponse(
                    response,
                    "Test case uploaded successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UploadTestCase");
        }
    }

    /// <summary>
    ///     Delete a test case (requires problem ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTestCase(long problemId, long id)
    {
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

            var testCase = await testCaseService.GetTestCaseAsync(id);
            if (testCase == null || testCase.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));
            }

            await testCaseService.DeleteTestCaseAsync(id, userId);

            logger.LogInformation(
                "Test case deleted successfully. ID: {TestCaseId}, Problem: {ProblemId}, User: {UserId}",
                id, problemId, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteTestCase");
        }
    }

    
}