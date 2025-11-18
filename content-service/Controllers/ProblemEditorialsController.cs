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
[Route("api/problems/{problemId}/editorials")]
public class ProblemEditorialsController(
    IEditorialService editorialService,
    IProblemService problemService,
    IEditorialMapper editorialMapper,
    ILogger<ProblemEditorialsController> logger) : BaseApiController
{
    /// <summary>
    ///     Get editorial for a problem
    /// </summary>
    [HttpGet]
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

            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(editorialMapper.ToResponse(editorial)));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetProblemEditorial");
        }
    }

    /// <summary>
    ///     Create an editorial for a problem (requires problem ownership or Admin role)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEditorial(long problemId, [FromBody] CreateEditorialRequest request)
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

            var editorial = await editorialService.CreateOrUpdateEditorialAsync(
                problemId,
                userId,
                request.Content,
                request.TimeComplexity,
                request.SpaceComplexity,
                request.VideoUrl);

            var response = editorialMapper.ToResponse(editorial);

            logger.LogInformation(
                "Editorial created/updated for problem {ProblemId} by user {UserId}",
                problemId, userId);

            return CreatedAtAction(
                nameof(GetProblemEditorial),
                new { problemId },
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
    ///     Get editorial by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEditorialById(long problemId, long id)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Problem not found."));
            }

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null || editorial.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
            }

            // Only show published editorials to non-authors
            if (!editorial.IsPublished)
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
                }

                var userId = GetUserIdFromClaims();
                var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(editorial.ProblemId, userId, IsAdmin());

                if (!isAuthorOrAdmin)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
                }
            }

            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(editorialMapper.ToResponse(editorial)));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetEditorialById");
        }
    }

    /// <summary>
    ///     Update editorial content (requires ownership or Admin role)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateEditorial(long problemId, long id, [FromBody] UpdateEditorialRequest request)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var userId = GetUserIdFromClaims();

            var existingEditorial = await editorialService.GetEditorialByIdAsync(id);
            if (existingEditorial == null || existingEditorial.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
            }

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(existingEditorial.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            var editorial = await editorialService.CreateOrUpdateEditorialAsync(
                existingEditorial.ProblemId,
                userId,
                request.Content,
                request.TimeComplexity,
                request.SpaceComplexity,
                request.VideoUrl);

            logger.LogInformation("Editorial {EditorialId} updated by user {UserId}", id, userId);

            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(
                editorialMapper.ToResponse(editorial),
                "Editorial updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UpdateEditorial");
        }
    }

    /// <summary>
    ///     Publish editorial (requires ownership or Admin role)
    /// </summary>
    [HttpPatch("{id}/publish")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishEditorial(long problemId, long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null || editorial.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
            }

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(editorial.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            await editorialService.PublishEditorialAsync(editorial.ProblemId, userId);

            logger.LogInformation("Editorial {EditorialId} published by user {UserId}", id, userId);

            var updatedEditorial = await editorialService.GetEditorialByIdAsync(id);
            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(
                editorialMapper.ToResponse(updatedEditorial!),
                "Editorial published successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "PublishEditorial");
        }
    }

    /// <summary>
    ///     Unpublish editorial (requires ownership or Admin role)
    /// </summary>
    [HttpPatch("{id}/unpublish")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnpublishEditorial(long problemId, long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null || editorial.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
            }

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(editorial.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            await editorialService.UnpublishEditorialAsync(editorial.ProblemId, userId);

            logger.LogInformation("Editorial {EditorialId} unpublished by user {UserId}", id, userId);

            var updatedEditorial = await editorialService.GetEditorialByIdAsync(id);
            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(
                editorialMapper.ToResponse(updatedEditorial!),
                "Editorial unpublished successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UnpublishEditorial");
        }
    }

    /// <summary>
    ///     Delete editorial (requires ownership or Admin role)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEditorial(long problemId, long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null || editorial.ProblemId != problemId)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
            }

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(editorial.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            await editorialService.DeleteEditorialAsync(editorial.ProblemId, userId);

            logger.LogInformation("Editorial {EditorialId} deleted by user {UserId}", id, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteEditorial");
        }
    }

    
}