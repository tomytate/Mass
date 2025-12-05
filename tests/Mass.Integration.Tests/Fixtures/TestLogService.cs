using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Logging;

namespace Mass.Integration.Tests.Fixtures;

public class TestLogService : ILogService
{
    public List<(LogLevel Level, string Category, string Message)> Logs { get; } = new();
    
    public void Log(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        Logs.Add((level, category, message));
    }
    
    public void LogTrace(string message, string category = "Application")
        => Log(LogLevel.Trace, category, message);
    
    public void LogDebug(string message, string category = "Application")
        => Log(LogLevel.Debug, category, message);
    
    public void LogInformation(string message, string category = "Application")
        => Log(LogLevel.Information, category, message);
    
    public void LogWarning(string message, string category = "Application")
        => Log(LogLevel.Warning, category, message);
    
    public void LogError(string message, Exception? exception = null, string category = "Application")
        => Log(LogLevel.Error, category, message, exception);
    
    public void LogCritical(string message, Exception? exception = null, string category = "Application")
        => Log(LogLevel.Critical, category, message, exception);
    
    public IEnumerable<LogEntry> GetLogs(int maxCount = 1000)
        => Enumerable.Empty<LogEntry>();
    
    public IEnumerable<LogEntry> GetLogsByLevel(LogLevel level, int maxCount = 1000)
        => Enumerable.Empty<LogEntry>();
    
    public IEnumerable<LogEntry> SearchLogs(string searchTerm, int maxCount = 1000)
        => Enumerable.Empty<LogEntry>();
    
    public void ClearLogs()
    {
        Logs.Clear();
    }
}
