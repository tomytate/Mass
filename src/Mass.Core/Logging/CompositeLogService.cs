using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Logging;

public class CompositeLogService : ILogService
{
    private readonly IEnumerable<ILogService> _sinks;

    public CompositeLogService(IEnumerable<ILogService> sinks)
    {
        _sinks = sinks.ToList();
    }

    public void Log(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        foreach (var sink in _sinks)
        {
            try
            {
                sink.Log(level, category, message, exception, properties);
            }
            catch
            {
                // Prevent one sink failure from stopping others
            }
        }
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
    {
        // Aggregate logs from all sinks that support retrieval
        return _sinks.SelectMany(s => s.GetLogs(maxCount))
                     .OrderByDescending(l => l.Timestamp)
                     .Take(maxCount);
    }

    public IEnumerable<LogEntry> GetLogsByLevel(LogLevel level, int maxCount = 1000)
    {
        return _sinks.SelectMany(s => s.GetLogsByLevel(level, maxCount))
                     .OrderByDescending(l => l.Timestamp)
                     .Take(maxCount);
    }

    public IEnumerable<LogEntry> SearchLogs(string searchTerm, int maxCount = 1000)
    {
        return _sinks.SelectMany(s => s.SearchLogs(searchTerm, maxCount))
                     .OrderByDescending(l => l.Timestamp)
                     .Take(maxCount);
    }

    public void ClearLogs()
    {
        foreach (var sink in _sinks)
        {
            sink.ClearLogs();
        }
    }
}
