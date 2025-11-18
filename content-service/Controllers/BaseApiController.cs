using System.Security.Claims;
using ContentService.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected long GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID not found in token claims.");
        }

        if (!long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format in token claims.");
        }

        return userId;
    }

    private IEnumerable<string> GetUserRolesFromClaims()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(User.FindAll("role").Select(c => c.Value));
    }

    protected bool IsProblemSetter()
    {
        var roles = GetUserRolesFromClaims();
        return roles.Any(r =>
            r.Equals("setter", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("moderator", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("super_admin", StringComparison.OrdinalIgnoreCase));
    }

    protected bool IsAdmin()
    {
        var roles = GetUserRolesFromClaims();
        return roles.Any(r =>
            r.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("super_admin", StringComparison.OrdinalIgnoreCase));
    }

    protected IActionResult HandleException(Exception ex, ILogger logger, string operation)
    {
        logger.LogError(ex, "Error during {Operation}: {Message}", operation, ex.Message);

        return ex switch
        {
            UnauthorizedAccessException => Unauthorized(ApiResponse<object>.ErrorResponse(ex.Message)),
            KeyNotFoundException => NotFound(ApiResponse<object>.ErrorResponse(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponse<object>.ErrorResponse(ex.Message)),
            ArgumentException => BadRequest(ApiResponse<object>.ErrorResponse(ex.Message)),
            _ => StatusCode(statusCode: 500, ApiResponse<object>.ErrorResponse("An unexpected error occurred. Please try again later."))
        };
    }

    protected IActionResult ValidateModelState()
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
        return null;
    }
}
