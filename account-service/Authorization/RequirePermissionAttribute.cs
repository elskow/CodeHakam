using System.Security.Claims;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using Casbin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccountService.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _action;
    private readonly bool _allowSelf;
    private readonly string _resource;

    public RequirePermissionAttribute(string resource, string action, bool allowSelf = false)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource cannot be null or empty", nameof(resource));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be null or empty", nameof(action));
        }

        _resource = resource;
        _action = action;
        _allowSelf = allowSelf;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.ErrorResponse("Authentication required")
            );
            return;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = user.FindFirst("username")?.Value ?? user.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.ErrorResponse("Invalid authentication token")
            );
            return;
        }

        // Allow self-access for /me endpoints (no route parameter) or when accessing own resource
        if (_allowSelf && (IsMeEndpoint(context) || IsSelfAccess(context, user)))
        {
            return;
        }

        // Use username for Casbin enforcement (role is assigned to username)
        if (string.IsNullOrEmpty(username))
        {
            username = userIdClaim; // Fallback to user ID if username not in claims
        }

        var enforcer = context.HttpContext.RequestServices.GetService<IEnforcer>();
        if (enforcer == null)
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.ErrorResponse("Authorization system not configured")
            )
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return;
        }

        var allowed = await enforcer.EnforceAsync(username, _resource, _action);

        if (!allowed)
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.ErrorResponse("Insufficient permissions")
            )
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    private bool IsMeEndpoint(AuthorizationFilterContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLower() ?? string.Empty;
        return path.Contains("/me");
    }

    private bool IsSelfAccess(AuthorizationFilterContext context, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return false;
        }

        if (!long.TryParse(userIdClaim, out var currentUserId))
        {
            return false;
        }

        var routeUserId = context.RouteData.Values["id"]?.ToString();
        if (string.IsNullOrEmpty(routeUserId))
        {
            return false;
        }

        if (!long.TryParse(routeUserId, out var targetUserId))
        {
            return false;
        }

        return currentUserId == targetUserId;
    }
}
