namespace Mass.Core.Services;

public class ResourceThresholds
{
    public double CpuWarning { get; set; } = 80.0;
    public double CpuCritical { get; set; } = 95.0;
    public double MemoryWarning { get; set; } = 80.0;
    public double MemoryCritical { get; set; } = 95.0;
    public double DiskWarning { get; set; } = 90.0;
    public double DiskCritical { get; set; } = 95.0;
}

public class ResourceAlertService : IDisposable
{
    private readonly IStatusService _statusService;
    private readonly INotificationService _notificationService;
    private readonly ResourceThresholds _thresholds;
    private readonly HashSet<string> _activeAlerts = new();
    private Timer? _checkTimer;

    public ResourceAlertService(
        IStatusService statusService, 
        INotificationService notificationService,
        ResourceThresholds? thresholds = null)
    {
        _statusService = statusService;
        _notificationService = notificationService;
        _thresholds = thresholds ?? new ResourceThresholds();
    }

    public void Start()
    {
        _checkTimer?.Dispose();
        _checkTimer = new Timer(CheckThresholds, null, 0, 5000);
    }

    public void Stop()
    {
        _checkTimer?.Change(Timeout.Infinite, 0);
    }

    private void CheckThresholds(object? state)
    {
        var status = _statusService.GetSystemStatus();

        CheckCpuThreshold(status.CpuUsagePercent);
        CheckMemoryThreshold(status.MemoryUsageBytes, status.TotalMemoryBytes);
        CheckDiskThresholds(status.Disks);
    }

    private void CheckCpuThreshold(double cpuPercent)
    {
        const string alertKey = "cpu";
        
        if (cpuPercent >= _thresholds.CpuCritical)
        {
            RaiseAlert(alertKey, "Critical CPU Usage", 
                $"CPU usage is at {cpuPercent:F1}%", NotificationSeverity.Error);
        }
        else if (cpuPercent >= _thresholds.CpuWarning)
        {
            RaiseAlert(alertKey, "High CPU Usage", 
                $"CPU usage is at {cpuPercent:F1}%", NotificationSeverity.Warning);
        }
        else
        {
            ClearAlert(alertKey);
        }
    }

    private void CheckMemoryThreshold(double used, double total)
    {
        const string alertKey = "memory";
        var percent = (used / total) * 100;
        
        if (percent >= _thresholds.MemoryCritical)
        {
            RaiseAlert(alertKey, "Critical Memory Usage", 
                $"Memory usage is at {percent:F1}%", NotificationSeverity.Error);
        }
        else if (percent >= _thresholds.MemoryWarning)
        {
            RaiseAlert(alertKey, "High Memory Usage", 
                $"Memory usage is at {percent:F1}%", NotificationSeverity.Warning);
        }
        else
        {
            ClearAlert(alertKey);
        }
    }

    private void CheckDiskThresholds(List<DiskInfo> disks)
    {
        foreach (var disk in disks)
        {
            var alertKey = $"disk_{disk.Name}";
            
            if (disk.UsagePercent >= _thresholds.DiskCritical)
            {
                RaiseAlert(alertKey, $"Critical Disk Space: {disk.Name}", 
                    $"Disk usage is at {disk.UsagePercent:F1}%", NotificationSeverity.Error);
            }
            else if (disk.UsagePercent >= _thresholds.DiskWarning)
            {
                RaiseAlert(alertKey, $"Low Disk Space: {disk.Name}", 
                    $"Disk usage is at {disk.UsagePercent:F1}%", NotificationSeverity.Warning);
            }
            else
            {
                ClearAlert(alertKey);
            }
        }
    }

    private void RaiseAlert(string key, string title, string message, NotificationSeverity severity)
    {
        if (!_activeAlerts.Contains(key))
        {
            _activeAlerts.Add(key);
            _notificationService.ShowNotification(title, message, severity);
        }
    }

    private void ClearAlert(string key)
    {
        _activeAlerts.Remove(key);
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
    }
}
