using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Interfaces;

public interface ILogService
{
    void Log(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null);
    void LogTrace(string message, string category = "Application");
    void LogDebug(string message, string category = "Application");
    void LogInformation(string message, string category = "Application");
    void LogWarning(string message, string category = "Application");
    void LogError(string message, Exception? exception = null, string category = "Application");
    void LogCritical(string message, Exception? exception = null, string category = "Application");
    
    IEnumerable<LogEntry> GetLogs(int maxCount = 1000);
    IEnumerable<LogEntry> GetLogsByLevel(LogLevel level, int maxCount = 1000);
    IEnumerable<LogEntry> SearchLogs(string searchTerm, int maxCount = 1000);
    void ClearLogs();
}
