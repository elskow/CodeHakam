using ContentService.DTOs;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestCasesController(
    ITestCaseService testCaseService,
    IProblemService problemService,
    ILogger<TestCasesController> logger)
    : BaseApiController
{
    /// <summary>
    /// Get test cases for a problem (sample test cases only for non-authors)
    /// </summary>
    [HttpGet("problem/{problemId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<TestCaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTestCases(long problemId, [FromQuery] bool samplesOnly = true)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            // Only problem authors and admins can see all test cases
            var requestSamplesOnly = samplesOnly;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserIdFromClaims();
                var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());

                if (isAuthorOrAdmin)
                {
                    requestSamplesOnly = false; // Can see all test cases
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
            return HandleException(ex, logger, "GetTestCases");
        }
    }

    /// <summary>
    /// Get a specific test case by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TestCaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTestCase(long id)
    {
        try
        {
            var testCase = await testCaseService.GetTestCaseAsync(id);
            if (testCase == null)
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
    /// Upload a test case for a problem (requires problem ownership or Admin role)
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TestCaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadTestCase([FromForm] UploadTestCaseRequest request)
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

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(request.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            // Upload test case
            var testCase = await testCaseService.UploadTestCaseAsync(
                problemId: request.ProblemId,
                inputFile: request.InputFile,
                outputFile: request.OutputFile,
                isSample: request.IsSample,
                testNumber: request.TestNumber,
                userId: userId);

            logger.LogInformation(
                "Test case uploaded successfully. ID: {TestCaseId}, Problem: {ProblemId}, User: {UserId}",
                testCase.Id, request.ProblemId, userId);

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

            return CreatedAtAction(nameof(GetTestCase), new { id = testCase.Id }, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UploadTestCase");
        }
    }

    /// <summary>
    /// Download test case input file
    /// </summary>
    [HttpGet("{id}/input")]
    [Authorize]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadInput(long id)
    {
        try
        {
            var testCase = await testCaseService.GetTestCaseAsync(id);
            if (testCase == null)
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
    /// Download test case output file
    /// </summary>
    [HttpGet("{id}/output")]
    [Authorize]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadOutput(long id)
    {
        try
        {
            var testCase = await testCaseService.GetTestCaseAsync(id);
            if (testCase == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
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
    /// Delete a test case (requires problem ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTestCase(long id)
    {
        try
        {
            var testCase = await testCaseService.GetTestCaseAsync(id);
            if (testCase == null)
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

            return Ok(ApiResponse<object>.SuccessResponse(new { id }, "Test case deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteTestCase");
        }
    }

    /// <summary>
    /// Get test case count for a problem
    /// </summary>
    [HttpGet("problem/{problemId}/count")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTestCaseCount(long problemId)
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
            return HandleException(ex, logger, "GetTestCaseCount");
        }
    }
}
