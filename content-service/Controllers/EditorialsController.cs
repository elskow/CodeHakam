using ContentService.DTOs.Common;
using ContentService.DTOs.Requests;
using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EditorialsController(
    IEditorialService editorialService,
    IProblemService problemService,
    ILogger<EditorialsController> logger)
    : BaseApiController
{
    /// <summary>
    ///     Get editorial by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EditorialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEditorialById(long id)
    {
        try
        {
            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
            }

            // Only show published editorials to non-authors
            if (!editorial.IsPublished)
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return NotFound(new { error = "Editorial not found." });
                }

                var userId = GetUserIdFromClaims();
                var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(editorial.ProblemId, userId, IsAdmin());

                if (!isAuthorOrAdmin)
                {
                    if (!editorial.IsPublished)
                    {
                        return NotFound(ApiResponse<object>.ErrorResponse("Editorial not found."));
                    }
                }
            }

            return Ok(ApiResponse<EditorialResponse>.SuccessResponse(MapToEditorialResponse(editorial)));
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
    public async Task<IActionResult> UpdateEditorial(long id, [FromBody] UpdateEditorialRequest request)
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

            var existingEditorial = await editorialService.GetEditorialByIdAsync(id);
            if (existingEditorial == null)
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
                MapToEditorialResponse(editorial),
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
    public async Task<IActionResult> PublishEditorial(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
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
                MapToEditorialResponse(updatedEditorial!),
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
    public async Task<IActionResult> UnpublishEditorial(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
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
                MapToEditorialResponse(updatedEditorial!),
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEditorial(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
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

            return Ok(ApiResponse<object>.SuccessResponse(new { id }, "Editorial deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "DeleteEditorial");
        }
    }

    private static EditorialResponse MapToEditorialResponse(Editorial editorial)
    {
        return new EditorialResponse
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
    }
}
