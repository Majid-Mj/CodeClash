namespace CodeClash.API.Common;

/// <summary>
/// Standard response envelope for every endpoint.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ApiResponse<T> Ok(T? data, string message = "Operation completed successfully")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(List<string> errors, string message = "Operation failed")
        => new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Fail(string error, string message = "Operation failed")
        => new() { Success = false, Message = message, Errors = new List<string> { error } };
}