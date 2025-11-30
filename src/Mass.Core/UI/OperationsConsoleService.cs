using System.Collections.Concurrent;

namespace Mass.Core.UI;

public class OperationsConsoleService : IOperationsConsoleService
{
    private readonly ConcurrentQueue<OperationLogEntry> _operations = new();
    private const int MaxOperations = 500;

    public event EventHandler<OperationLogEntry>? OperationLogged;

    public void LogOperation(string operation, string message, OperationLogLevel level = OperationLogLevel.Info)
    {
        var entry = new OperationLogEntry
        {
            Timestamp = DateTime.Now,
            Operation = operation,
            Message = message,
            Level = level,
            Status = level.ToString()
        };

        _operations.Enqueue(entry);
        
        while (_operations.Count > MaxOperations)
        {
            _operations.TryDequeue(out _);
        }

        OperationLogged?.Invoke(this, entry);
    }

    public void LogSuccess(string operation, string message)
    {
        LogOperation(operation, message, OperationLogLevel.Success);
    }

    public void LogWarning(string operation, string message)
    {
        LogOperation(operation, message, OperationLogLevel.Warning);
    }

    public void LogError(string operation, string message, Exception? exception = null)
    {
        var details = exception?.ToString() ?? string.Empty;
        var entry = new OperationLogEntry
        {
            Timestamp = DateTime.Now,
            Operation = operation,
            Message = message,
            Level = OperationLogLevel.Error,
            Status = "Error",
            Details = details
        };

        _operations.Enqueue(entry);
        
        while (_operations.Count > MaxOperations)
        {
            _operations.TryDequeue(out _);
        }

        OperationLogged?.Invoke(this, entry);
    }

    public IEnumerable<OperationLogEntry> GetRecentOperations(int count = 100)
    {
        return _operations.Reverse().Take(count);
    }

    public void Clear()
    {
        _operations.Clear();
    }
}
