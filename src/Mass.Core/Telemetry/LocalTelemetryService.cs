using System.Collections.Concurrent;
using System.Text.Json;
using Mass.Core.Abstractions;
using Mass.Core.Configuration;

namespace Mass.Core.Telemetry;

public class LocalTelemetryService : ITelemetryService
{
    private readonly IConfigurationService _configService;
    private readonly ConcurrentQueue<TelemetryEvent> _eventBuffer = new();
    private readonly string _logDirectory;
    private readonly Timer _flushTimer;
    private const int FlushIntervalMs = 60000; // 1 minute

    public bool IsEnabled => _configService.Get<AppSettings>("AppSettings", new AppSettings())?.Telemetry?.Enabled ?? false;

    public LocalTelemetryService(IConfigurationService configService)
    {
        _configService = configService;
        _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "logs", "telemetry");
        Directory.CreateDirectory(_logDirectory);
        _flushTimer = new Timer(async _ => await FlushAsync(), null, FlushIntervalMs, FlushIntervalMs);
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        if (!IsEnabled) return;

        var telemetryEvent = new TelemetryEvent
        {
            Timestamp = DateTime.UtcNow,
            Type = "Event",
            Name = eventName,
            Properties = SanitizeProperties(properties)
        };

        _eventBuffer.Enqueue(telemetryEvent);
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        if (!IsEnabled) return;

        var props = properties ?? new Dictionary<string, string>();
        props["Message"] = exception.Message;
        props["StackTrace"] = exception.StackTrace ?? string.Empty;
        props["Source"] = exception.Source ?? string.Empty;

        var telemetryEvent = new TelemetryEvent
        {
            Timestamp = DateTime.UtcNow,
            Type = "Exception",
            Name = exception.GetType().Name,
            Properties = SanitizeProperties(props)
        };

        _eventBuffer.Enqueue(telemetryEvent);
    }

    public void TrackPageView(string pageName)
    {
        if (!IsEnabled) return;

        var telemetryEvent = new TelemetryEvent
        {
            Timestamp = DateTime.UtcNow,
            Type = "PageView",
            Name = pageName
        };

        _eventBuffer.Enqueue(telemetryEvent);
    }

    public async Task FlushAsync()
    {
        if (_eventBuffer.IsEmpty) return;

        var eventsToWrite = new List<TelemetryEvent>();
        while (_eventBuffer.TryDequeue(out var evt))
        {
            eventsToWrite.Add(evt);
        }

        if (eventsToWrite.Count == 0) return;

        var fileName = $"telemetry_{DateTime.UtcNow:yyyy-MM-dd}.json";
        var filePath = Path.Combine(_logDirectory, fileName);

        try
        {
            var json = JsonSerializer.Serialize(eventsToWrite, new JsonSerializerOptions { WriteIndented = true });
            
            // Append to file (reading existing first if needed, but for simplicity just appending lines or writing array)
            // Since JSON array is tricky to append to, we'll write line-delimited JSON (NDJSON) or just append to a list if we read it first.
            // For robustness and simplicity in local logging, let's use NDJSON (Newlines Delimited JSON) which is easier to append.
            
            var lines = eventsToWrite.Select(e => JsonSerializer.Serialize(e));
            await File.AppendAllLinesAsync(filePath, lines);
        }
        catch
        {
            // Fail silently for telemetry
        }
    }

    private Dictionary<string, string>? SanitizeProperties(IDictionary<string, string>? properties)
    {
        if (properties == null) return null;

        var sanitized = new Dictionary<string, string>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var kvp in properties)
        {
            var value = kvp.Value;
            if (!string.IsNullOrEmpty(value) && value.Contains(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            }
            sanitized[kvp.Key] = value;
        }

        return sanitized;
    }

    private class TelemetryEvent
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string>? Properties { get; set; }
    }
}
