using System.Net;
using System.Text.Json;
using ContentService.DTOs;

namespace ContentService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ArgumentException argEx => CreateResponse(
                HttpStatusCode.BadRequest,
                "Invalid argument",
                argEx.Message
            ),
            UnauthorizedAccessException => CreateResponse(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to access this resource"
            ),
            InvalidOperationException invOpEx => CreateResponse(
                HttpStatusCode.BadRequest,
                "Invalid operation",
                invOpEx.Message
            ),
            KeyNotFoundException => CreateResponse(
                HttpStatusCode.NotFound,
                "Resource not found",
                "The requested resource was not found"
            ),
            _ => CreateResponse(
                HttpStatusCode.InternalServerError,
                "Internal server error",
                "An unexpected error occurred. Please try again later."
            )
        };

        context.Response.StatusCode = (int)response.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response.ApiResponse, options);
        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode StatusCode, ApiResponse<object> ApiResponse) CreateResponse(
        HttpStatusCode statusCode,
        string message,
        string? detail = null)
    {
        var apiResponse = ApiResponse<object>.ErrorResponse(
            message,
            detail != null
                ? new Dictionary<string, string[]> { { "detail", new[] { detail } } }
                : null
        );

        return (statusCode, apiResponse);
    }
}
