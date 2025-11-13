namespace AccountService.DTOs.Common;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> ErrorResponse(string message, Dictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public record PaginatedResponse<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
