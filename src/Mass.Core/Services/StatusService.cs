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

    private readonly IIpcService? _ipcService;
    private readonly Mass.Core.Plugins.PluginLifecycleManager? _pluginManager;

    public StatusService(
        IIpcService ipcService,
        Mass.Core.Plugins.PluginLifecycleManager pluginManager)
    {
        _ipcService = ipcService;
        _pluginManager = pluginManager;
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

    // Default constructor for design-time or fallback
    public StatusService()
    {
        _currentProcess = Process.GetCurrentProcess();
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
        var statuses = new List<ModuleStatus>();

        // Check MassBoot (IPC Server)
        bool isServerRunning = _ipcService?.IsServerRunning ?? false;
        statuses.Add(new ModuleStatus 
        { 
            Name = "MassBoot Server", 
            Icon = "🖥️", 
            Status = isServerRunning ? "Running" : "Stopped", 
            Color = isServerRunning ? "#10B981" : "#6B7280" 
        });

        // Check ProUSB (Plugin)
        var proUsb = _pluginManager?.LoadedPlugins.Values.FirstOrDefault(p => p.Manifest.Id.Equals("prousb", StringComparison.OrdinalIgnoreCase));
        bool isProUsbReady = proUsb?.State == Mass.Core.Plugins.PluginState.Running;
        statuses.Add(new ModuleStatus 
        { 
            Name = "ProUSB Engine", 
            Icon = "💾", 
            Status = isProUsbReady ? "Ready" : "Inactive", 
            Color = isProUsbReady ? "#10B981" : "#6B7280" 
        });

        // Check Orchestrator (Workflows)
        statuses.Add(new ModuleStatus 
        { 
            Name = "Orchestrator", 
            Icon = "⚙️", 
            Status = "Ready", 
            Color = "#10B981" 
        });

        return statuses;
    }

    public SystemStatus GetSystemStatus()
    {
        double cpuUsage = 0;
        try
        {
            if (OperatingSystem.IsWindows() && _cpuCounter != null)
            {
                cpuUsage = GetCpuUsageWindows();
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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private double GetCpuUsageWindows()
    {
        return _cpuCounter?.NextValue() ?? 0;
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _cpuCounter?.Dispose();
    }
}
