using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Logging;

public class ConsoleLogService : ILogService
{
    public void Log(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = GetColorForLevel(level);

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] [{level}] [{category}] {message}");

        if (exception != null)
        {
            Console.WriteLine(exception);
        }

        Console.ForegroundColor = originalColor;
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

    public IEnumerable<LogEntry> GetLogs(int maxCount = 1000) => Enumerable.Empty<LogEntry>();
    public IEnumerable<LogEntry> GetLogsByLevel(LogLevel level, int maxCount = 1000) => Enumerable.Empty<LogEntry>();
    public IEnumerable<LogEntry> SearchLogs(string searchTerm, int maxCount = 1000) => Enumerable.Empty<LogEntry>();
    public void ClearLogs() { }

    private ConsoleColor GetColorForLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.Blue,
        LogLevel.Information => ConsoleColor.Green,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };
}
