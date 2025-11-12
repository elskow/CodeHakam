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
    ///     Get editorial for a problem
    /// </summary>
    [HttpGet("problem/{problemId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEditorial(long problemId)
    {
        try
        {
            var problem = await problemService.GetProblemAsync(problemId);
            if (problem == null)
            {
                return NotFound(new { error = "Problem not found." });
            }

            var editorial = await editorialService.GetEditorialAsync(problemId);
            if (editorial == null)
            {
                return NotFound(new { error = "Editorial not found for this problem." });
            }

            // Only show published editorials to non-authors
            if (!editorial.IsPublished)
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return NotFound(new { error = "Editorial not found for this problem." });
                }

                var userId = GetUserIdFromClaims();
                var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(problemId, userId, IsAdmin());

                if (!isAuthorOrAdmin)
                {
                    return NotFound(new { error = "Editorial not found for this problem." });
                }
            }

            return Ok(MapToEditorialResponse(editorial));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetEditorial");
        }
    }

    /// <summary>
    ///     Get editorial by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEditorialById(long id)
    {
        try
        {
            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
            {
                return NotFound(new { error = "Editorial not found." });
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
                    return NotFound(new { error = "Editorial not found." });
                }
            }

            return Ok(MapToEditorialResponse(editorial));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "GetEditorialById");
        }
    }

    /// <summary>
    ///     Create or update editorial for a problem (requires problem ownership or Admin role)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEditorial([FromBody] CreateEditorialRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Verify problem exists
            var problem = await problemService.GetProblemAsync(request.ProblemId);
            if (problem == null)
            {
                return NotFound(new { error = "Problem not found." });
            }

            // Check authorization
            var isAuthorOrAdmin = await problemService.IsAuthorOrAdminAsync(request.ProblemId, userId, IsAdmin());
            if (!isAuthorOrAdmin)
            {
                return Forbid();
            }

            var editorial = await editorialService.CreateOrUpdateEditorialAsync(
                request.ProblemId,
                userId,
                request.Content,
                request.TimeComplexity,
                request.SpaceComplexity,
                request.VideoUrl);

            logger.LogInformation(
                "Editorial created/updated for problem {ProblemId} by user {UserId}",
                request.ProblemId, userId);

            return CreatedAtAction(
                nameof(GetEditorialById),
                new { id = editorial.Id },
                MapToEditorialResponse(editorial));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "CreateEditorial");
        }
    }

    /// <summary>
    ///     Update editorial content (requires ownership or Admin role)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEditorial(long id, [FromBody] CreateEditorialRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var existingEditorial = await editorialService.GetEditorialByIdAsync(id);
            if (existingEditorial == null)
            {
                return NotFound(new { error = "Editorial not found." });
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

            return Ok(MapToEditorialResponse(editorial));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "UpdateEditorial");
        }
    }

    /// <summary>
    ///     Publish editorial (requires ownership or Admin role)
    /// </summary>
    [HttpPost("{id}/publish")]
    [Authorize]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishEditorial(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
            {
                return NotFound(new { error = "Editorial not found." });
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
            return Ok(MapToEditorialResponse(updatedEditorial!));
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, "PublishEditorial");
        }
    }

    /// <summary>
    ///     Unpublish editorial (requires ownership or Admin role)
    /// </summary>
    [HttpPost("{id}/unpublish")]
    [Authorize]
    [ProducesResponseType(typeof(EditorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnpublishEditorial(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
            {
                return NotFound(new { error = "Editorial not found." });
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
            return Ok(MapToEditorialResponse(updatedEditorial!));
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEditorial(long id)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            var editorial = await editorialService.GetEditorialByIdAsync(id);
            if (editorial == null)
            {
                return NotFound(new { error = "Editorial not found." });
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
