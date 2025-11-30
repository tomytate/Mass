namespace Mass.Core.Services;

public interface IStatusService
{
    IEnumerable<ModuleStatus> GetModuleStatuses();
    SystemStatus GetSystemStatus();
    
    void StartMonitoring();
    void StopMonitoring();
    event EventHandler<SystemStatus> StatusUpdated;
}

public class ModuleStatus
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Ready";
    public string Icon { get; set; } = "📦";
    public string Color { get; set; } = "#10B981"; // Green
}

public class SystemStatus
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsageBytes { get; set; }
    public double TotalMemoryBytes { get; set; }
    public TimeSpan Uptime { get; set; }
    public List<DiskInfo> Disks { get; set; } = new();
    public NetworkStatus Network { get; set; } = new();
    
    public string CpuDisplay => $"{CpuUsagePercent:F1}%";
    public string MemoryDisplay => $"{MemoryUsageBytes / 1024 / 1024:F0} MB / {TotalMemoryBytes / 1024 / 1024:F0} MB";
    public string UptimeDisplay => Uptime.ToString(@"dd\.hh\:mm\:ss");
}

public class NetworkStatus
{
    public double BytesSentPerSecond { get; set; }
    public double BytesReceivedPerSecond { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    
    public string UploadSpeedDisplay => FormatBytes(BytesSentPerSecond) + "/s";
    public string DownloadSpeedDisplay => FormatBytes(BytesReceivedPerSecond) + "/s";
    
    private static string FormatBytes(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes /= 1024;
        }
        return $"{bytes:F2} {sizes[order]}";
    }
}

public class DiskInfo
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double TotalSpaceBytes { get; set; }
    public double FreeSpaceBytes { get; set; }
    public double UsagePercent => TotalSpaceBytes > 0 ? (1 - (FreeSpaceBytes / TotalSpaceBytes)) * 100 : 0;
}
