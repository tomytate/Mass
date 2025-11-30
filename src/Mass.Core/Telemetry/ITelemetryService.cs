namespace Mass.Core.Telemetry;

public interface ITelemetryService
{
    bool IsEnabled { get; }
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
    void TrackPageView(string pageName);
    Task FlushAsync();
}
