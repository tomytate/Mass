using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Mass.Core.Services;

public class StatusService : IStatusService, IDisposable
{
    private readonly Process _currentProcess;
    private Timer? _monitoringTimer;
    private readonly PerformanceCounter? _cpuCounter;
    private long _previousBytesSent;
    private long _previousBytesReceived;
    private DateTime _previousNetworkCheck = DateTime.Now;
    
    public event EventHandler<SystemStatus>? StatusUpdated;

    public StatusService()
    {
        _currentProcess = Process.GetCurrentProcess();
        
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            
            InitializeNetworkCounters();
        }
        catch
        {
        }
    }

    public void StartMonitoring()
    {
        _monitoringTimer?.Dispose();
        _monitoringTimer = new Timer(OnMonitoringTick, null, 0, 1000);
    }

    public void StopMonitoring()
    {
        _monitoringTimer?.Change(Timeout.Infinite, 0);
    }

    private void OnMonitoringTick(object? state)
    {
        var status = GetSystemStatus();
        StatusUpdated?.Invoke(this, status);
    }

    public IEnumerable<ModuleStatus> GetModuleStatuses()
    {
        return new[]
        {
            new ModuleStatus { Name = "ProUSB", Icon = "💾", Status = "Ready", Color = "#10B981" },
            new ModuleStatus { Name = "MassBoot", Icon = "🖥️", Status = "Active", Color = "#3B82F6" },
            new ModuleStatus { Name = "Orchestrator", Icon = "⚙️", Status = "Idle", Color = "#6B7280" }
        };
    }

    public SystemStatus GetSystemStatus()
    {
        double cpuUsage = 0;
        try
        {
            if (_cpuCounter != null)
            {
                cpuUsage = _cpuCounter.NextValue();
            }
        }
        catch { }

        var disks = new List<DiskInfo>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                disks.Add(new DiskInfo
                {
                    Name = drive.Name,
                    Label = drive.VolumeLabel,
                    TotalSpaceBytes = drive.TotalSize,
                    FreeSpaceBytes = drive.TotalFreeSpace
                });
            }
        }
        catch { }

        return new SystemStatus
        {
            CpuUsagePercent = cpuUsage,
            MemoryUsageBytes = _currentProcess.WorkingSet64,
            TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            Uptime = DateTime.Now - _currentProcess.StartTime,
            Disks = disks,
            Network = GetNetworkStatus()
        };
    }

    private void InitializeNetworkCounters()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPv4Statistics();
                _previousBytesSent += stats.BytesSent;
                _previousBytesReceived += stats.BytesReceived;
            }
        }
        catch { }
    }

    private NetworkStatus GetNetworkStatus()
    {
        var networkStatus = new NetworkStatus();
        
        try
        {
            long currentBytesSent = 0;
            long currentBytesReceived = 0;
            
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPv4Statistics();
                currentBytesSent += stats.BytesSent;
                currentBytesReceived += stats.BytesReceived;
            }
            
            var now = DateTime.Now;
            var elapsed = (now - _previousNetworkCheck).TotalSeconds;
            
            if (elapsed > 0)
            {
                networkStatus.BytesSentPerSecond = (currentBytesSent - _previousBytesSent) / elapsed;
                networkStatus.BytesReceivedPerSecond = (currentBytesReceived - _previousBytesReceived) / elapsed;
            }
            
            networkStatus.TotalBytesSent = currentBytesSent;
            networkStatus.TotalBytesReceived = currentBytesReceived;
            
            _previousBytesSent = currentBytesSent;
            _previousBytesReceived = currentBytesReceived;
            _previousNetworkCheck = now;
        }
        catch { }
        
        return networkStatus;
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _cpuCounter?.Dispose();
    }
}
