using ContentService.DTOs.Common;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/problems/{problemId}/test-cases")]
public class TestCasesController(
    ITestCaseService testCaseService,
    IProblemService problemService,
    ILogger<TestCasesController> logger)
    : BaseApiController
{
    /// <summary>
    ///     Get a specific test case by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TestCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetTestCase(long problemId, long id)
        {
            try
            {
                var testCase = await testCaseService.GetTestCaseAsync(id);
                if (testCase == null || testCase.ProblemId != problemId)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));
                }

                // Check authorization - only author/admin or for sample test cases
                if (!testCase.IsSample)
                {
                    var userId = GetUserIdFromClaims();
                    var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(testCase.ProblemId, userId, IsAdmin());

                    if (!isAuthorOrAdmin)
                    {
                        return Forbid();
                    }
                }

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

                return Ok(ApiResponse<TestCaseResponse>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                return HandleException(ex, logger, "GetTestCase");
            }
        }

    /// <summary>
    ///     Download test case input file
    /// </summary>
    [HttpGet("{id}/input")]
    [Authorize]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> DownloadInput(long problemId, long id)
        {
            try
            {
                var testCase = await testCaseService.GetTestCaseAsync(id);
                if (testCase == null || testCase.ProblemId != problemId)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));
                }

                // Check authorization
                if (!testCase.IsSample)
                {
                    var userId = GetUserIdFromClaims();
                    var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(testCase.ProblemId, userId, IsAdmin());

                    if (!isAuthorOrAdmin)
                    {
                        return Forbid();
                    }
                }

                var (stream, contentType, fileName) = await testCaseService.DownloadTestCaseInputAsync(id);

                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, logger, "DownloadInput");
            }
        }

    /// <summary>
    ///     Download test case output file
    /// </summary>
    [HttpGet("{id}/output")]
    [Authorize]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> DownloadOutput(long problemId, long id)
        {
            try
            {
                var testCase = await testCaseService.GetTestCaseAsync(id);
                if (testCase == null || testCase.ProblemId != problemId)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));
                }

                // Check authorization
                if (!testCase.IsSample)
                {
                    var userId = GetUserIdFromClaims();
                    var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(testCase.ProblemId, userId, IsAdmin());

                    if (!isAuthorOrAdmin)
                    {
                        return Forbid();
                    }
                }

                var (stream, contentType, fileName) = await testCaseService.DownloadTestCaseOutputAsync(id);

                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, logger, "DownloadOutput");
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
            var testCase = await testCaseService.GetTestCaseAsync(id);
            if (testCase == null || testCase.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Test case not found."));
            }

            var userId = GetUserIdFromClaims();

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(testCase.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            await testCaseService.DeleteTestCaseAsync(id, userId);

            logger.LogInformation(
                "Test case deleted successfully. ID: {TestCaseId}, User: {UserId}",
                id, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteTestCase");
        }
    }
}
