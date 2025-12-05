namespace Mass.Agent;

public class AgentConfiguration
{
    public string AgentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string AgentName { get; set; } = Environment.MachineName;
    public string DashboardUrl { get; set; } = "http://localhost:5000";
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    public static AgentConfiguration LoadFromEnvironment()
    {
        return new AgentConfiguration
        {
            AgentId = Environment.GetEnvironmentVariable("MASS_AGENT_ID") ?? Guid.NewGuid().ToString("N")[..8],
            AgentName = Environment.GetEnvironmentVariable("MASS_AGENT_NAME") ?? Environment.MachineName,
            DashboardUrl = Environment.GetEnvironmentVariable("MASS_DASHBOARD_URL") ?? "http://localhost:5000",
            HeartbeatIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("MASS_HEARTBEAT_INTERVAL"), out var interval) ? interval : 30,
            Tags = (Environment.GetEnvironmentVariable("MASS_AGENT_TAGS") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
        };
    }
}
