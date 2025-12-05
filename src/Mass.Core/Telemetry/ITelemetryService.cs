using Mass.Spec.Contracts.Logging;

namespace Mass.Core.Telemetry;

public interface ITelemetryService
{
    bool ConsentGiven { get; set; }
    void TrackEvent(TelemetryEvent e);
    Task FlushAsync();
}
