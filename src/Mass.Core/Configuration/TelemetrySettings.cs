namespace Mass.Core.Configuration;

public class TelemetrySettings
{
    public bool Enabled { get; set; } = false;
    public bool ConsentDecisionMade { get; set; } = false;
    public string InstallationId { get; set; } = Guid.NewGuid().ToString();
}
