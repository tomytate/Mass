namespace Mass.Core.Configuration;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public string? StartupModule { get; set; }
    public bool CheckForUpdates { get; set; } = true;
    public bool MinimizeToTray { get; set; } = false;
    public TelemetrySettings Telemetry { get; set; } = new();
}
