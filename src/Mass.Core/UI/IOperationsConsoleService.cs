namespace Mass.Core.UI;

public class OperationLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public OperationLogLevel Level { get; set; }
    public string Details { get; set; } = string.Empty;
}

public enum OperationLogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public interface IOperationsConsoleService
{
    void LogOperation(string operation, string message, OperationLogLevel level = OperationLogLevel.Info);
    void LogSuccess(string operation, string message);
    void LogWarning(string operation, string message);
    void LogError(string operation, string message, Exception? exception = null);
    IEnumerable<OperationLogEntry> GetRecentOperations(int count = 100);
    event EventHandler<OperationLogEntry> OperationLogged;
    void Clear();
}
