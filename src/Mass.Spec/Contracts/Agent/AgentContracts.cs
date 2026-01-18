namespace Mass.Spec.Contracts.Agent;

public class AgentRegistrationRequest
{
    public string Hostname { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}

public class AgentRegistrationResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
}

public class AgentHeartbeatRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string Status { get; set; } = "Idle"; // Idle, Busy, Error, Offline
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public string? CurrentJobId { get; set; }
}

public class AgentHeartbeatResponse
{
    public bool Success { get; set; }
    public AgentJob? PendingJob { get; set; }
}

public class AgentJob
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty; // "Deploy", "Script", "Inventory"
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class AgentJobStatusUpdate
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Running", "Completed", "Failed"
    public string? Output { get; set; }
    public int ProgressPercent { get; set; }
}
