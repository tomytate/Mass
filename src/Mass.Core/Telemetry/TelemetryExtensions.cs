using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Telemetry;

public static class TelemetryExtensions
{
    public static void TrackEvent(this ITelemetryService telemetry, string eventName, string source = "Application", Dictionary<string, object>? properties = null)
    {
        telemetry.TrackEvent(new TelemetryEvent
        {
            EventType = "Event",
            Source = source,
            Name = eventName,
            Timestamp = DateTime.UtcNow,
            Properties = properties ?? new Dictionary<string, object>()
        });
    }

    public static void TrackException(this ITelemetryService telemetry, Exception exception, string source = "Application", Dictionary<string, object>? properties = null)
    {
        var props = properties ?? new Dictionary<string, object>();
        props["Message"] = exception.Message;
        props["StackTrace"] = exception.StackTrace ?? string.Empty;
        if (!string.IsNullOrEmpty(exception.Source))
        {
            props["ExceptionSource"] = exception.Source;
        }

        telemetry.TrackEvent(new TelemetryEvent
        {
            EventType = "Exception",
            Source = source,
            Name = exception.GetType().Name,
            Timestamp = DateTime.UtcNow,
            Properties = props
        });
    }

    public static void TrackPageView(this ITelemetryService telemetry, string pageName, string source = "Application")
    {
        telemetry.TrackEvent(new TelemetryEvent
        {
            EventType = "PageView",
            Source = source,
            Name = pageName,
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>()
        });
    }
}
