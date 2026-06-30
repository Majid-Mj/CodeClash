namespace CodeClash.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = new();

    private Result() { }

    public static Result<T> Success(T data, string message = "Operation completed successfully")
        => new() { IsSuccess = true, Data = data, Message = message };

    public static Result<T> Failure(string error, string message = "Operation failed")
        => new() { IsSuccess = false, Message = message, Errors = new List<string> { error } };

    public static Result<T> Failure(List<string> errors, string message = "Operation failed")
        => new() { IsSuccess = false, Message = message, Errors = errors };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = new();

    public static Result Success(string message = "Operation completed successfully")
        => new() { IsSuccess = true, Message = message };

    public static Result Failure(string error, string message = "Operation failed")
        => new() { IsSuccess = false, Message = message, Errors = new List<string> { error } };
}