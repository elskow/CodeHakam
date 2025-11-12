using System.Security.Claims;
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

    private string GetUserRoleFromClaims()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

        return roleClaim ?? "User";
    }

    protected bool IsProblemSetter()
    {
        var role = GetUserRoleFromClaims();
        return role.Equals("ProblemSetter", StringComparison.OrdinalIgnoreCase) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    protected bool IsAdmin()
    {
        var role = GetUserRoleFromClaims();
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    protected IActionResult HandleException(Exception ex, ILogger logger, string operation)
    {
        logger.LogError(ex, "Error during {Operation}: {Message}", operation, ex.Message);

        return ex switch
        {
            UnauthorizedAccessException => Unauthorized(new { error = ex.Message }),
            KeyNotFoundException => NotFound(new { error = ex.Message }),
            InvalidOperationException => BadRequest(new { error = ex.Message }),
            ArgumentException => BadRequest(new { error = ex.Message }),
            _ => StatusCode(statusCode: 500, new { error = "An unexpected error occurred. Please try again later." })
        };
    }
}
