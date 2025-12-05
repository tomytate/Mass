using Mass.Core.Telemetry;
using Mass.Spec.Contracts.Logging;

namespace Mass.Integration.Tests.Fixtures;

public class TestTelemetryService : ITelemetryService
{
    public List<TelemetryEvent> Events { get; } = new();
    public bool ConsentGiven { get; set; } = true;
    
    public void TrackEvent(TelemetryEvent e)
    {
        if (ConsentGiven)
        {
            Events.Add(e);
        }
    }
    
    public Task FlushAsync()
    {
        return Task.CompletedTask;
    }
}
