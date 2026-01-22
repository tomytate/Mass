using System.Collections.Concurrent;
using System.Text.Json;
using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Telemetry;

public class LocalTelemetryService : ITelemetryService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ConcurrentQueue<TelemetryEvent> _eventBuffer = new();
    private readonly string _logDirectory;
    private readonly Timer _flushTimer;
    private const int FlushIntervalMs = 60000;

    public bool ConsentGiven
    {
        get => _configService.Get("Telemetry.ConsentDecisionMade", false) && 
               _configService.Get("Telemetry.Enabled", false);
        set
        {
            _configService.Set("Telemetry.ConsentDecisionMade", true);
            _configService.Set("Telemetry.Enabled", value);
        }
    }

    public LocalTelemetryService(IConfigurationService configService) 
        : this(configService, null)
    {
    }

    public LocalTelemetryService(IConfigurationService configService, string? customLogDirectory)
    {
        _configService = configService;
        
        if (!string.IsNullOrEmpty(customLogDirectory))
        {
            _logDirectory = customLogDirectory;
        }
        else
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "MassSuite", 
                "logs", 
                "telemetry");
        }
        
        Directory.CreateDirectory(_logDirectory);
        _flushTimer = new Timer(async _ => await FlushAsync(), null, FlushIntervalMs, FlushIntervalMs);
    }

    public void TrackEvent(TelemetryEvent e)
    {
        if (!ConsentGiven) return;

        e.Timestamp = DateTime.UtcNow;
        e.Properties = SanitizeProperties(e.Properties);
        _eventBuffer.Enqueue(e);
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
            var lines = eventsToWrite.Select(e => JsonSerializer.Serialize(e));
            await File.AppendAllLinesAsync(filePath, lines);
        }
        catch
        {
            // Fail silently for telemetry
        }
    }

    private Dictionary<string, object> SanitizeProperties(Dictionary<string, object>? properties)
    {
        if (properties == null) return new Dictionary<string, object>();

        var sanitized = new Dictionary<string, object>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var kvp in properties)
        {
            var value = kvp.Value?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(value) && value.Contains(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            }
            sanitized[kvp.Key] = value;
        }

        return sanitized;
    }

    public void Dispose()
    {
        _flushTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
