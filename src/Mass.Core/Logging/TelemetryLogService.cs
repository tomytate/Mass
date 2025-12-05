using Mass.Core.Interfaces;
using Mass.Core.Telemetry;
using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Logging;

public class TelemetryLogService : ILogService
{
    private readonly ITelemetryService _telemetryService;
    private readonly LogLevel _minLevel;

    public TelemetryLogService(ITelemetryService telemetryService, LogLevel minLevel = LogLevel.Warning)
    {
        _telemetryService = telemetryService;
        _minLevel = minLevel;
    }

    public void Log(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        if (level < _minLevel) return;

        var telemetryEvent = new TelemetryEvent
        {
            EventType = exception != null ? "Exception" : "Log",
            Source = category,
            Name = exception != null ? exception.GetType().Name : $"Log_{level}",
            Timestamp = DateTime.UtcNow,
            Properties = properties ?? new Dictionary<string, object>()
        };

        telemetryEvent.Properties["Level"] = level.ToString();
        telemetryEvent.Properties["Message"] = message;

        if (exception != null)
        {
            telemetryEvent.Properties["ExceptionMessage"] = exception.Message;
            telemetryEvent.Properties["StackTrace"] = exception.StackTrace ?? string.Empty;
            if (!string.IsNullOrEmpty(exception.Source))
            {
                telemetryEvent.Properties["ExceptionSource"] = exception.Source;
            }
        }

        _telemetryService.TrackEvent(telemetryEvent);
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
}
