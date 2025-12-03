using Microsoft.Extensions.Logging;

namespace Mass.Core.Logging;

public static class SafeExecutor
{
    public static async Task<T?> ExecuteAsync<T>(
        Func<Task<T>> action,
        ILogger logger,
        string operationName,
        T? defaultValue = default)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("{Operation} was cancelled", operationName);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            return defaultValue;
        }
    }

    public static async Task<bool> ExecuteAsync(
        Func<Task> action,
        ILogger logger,
        string operationName)
    {
        try
        {
            await action();
            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("{Operation} was cancelled", operationName);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            return false;
        }
    }

    public static T? Execute<T>(
        Func<T> action,
        ILogger logger,
        string operationName,
        T? defaultValue = default)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            return defaultValue;
        }
    }

    public static bool Execute(
        Action action,
        ILogger logger,
        string operationName)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            return false;
        }
    }
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string? error, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Exception = exception;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error, Exception? ex = null) => new(false, default, error, ex);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
