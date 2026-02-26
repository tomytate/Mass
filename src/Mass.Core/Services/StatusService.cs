using System.Diagnostics;
using System.Net.NetworkInformation;
using Mass.Core.Interfaces;

namespace Mass.Core.Services;

public class StatusService : IStatusService, IDisposable
{
    private readonly Process _currentProcess;
    private PeriodicTimer? _periodicTimer;
    private CancellationTokenSource? _cts;
    private Task? _monitoringTask;
    
    private readonly PerformanceCounter? _cpuCounter;
    private long _previousBytesSent;
    private long _previousBytesReceived;
    private DateTime _previousNetworkCheck = DateTime.Now;

    public event EventHandler<SystemStatus>? StatusUpdated;

    private readonly IIpcService? _ipcService;
    private readonly Mass.Core.Plugins.PluginLifecycleManager? _pluginManager;
    private readonly ILogService? _logger;

    public StatusService(
        IIpcService ipcService,
        Mass.Core.Plugins.PluginLifecycleManager pluginManager,
        ILogService? logger = null)
    {
        _ipcService = ipcService;
        _pluginManager = pluginManager;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize system counters", ex, "StatusService");
            // Suppress initialization errors for counters
        }
    }

    // Default constructor for design-time or fallback
    public StatusService()
    {
        _currentProcess = Process.GetCurrentProcess();
    }

    public void StartMonitoring()
    {
        if (_monitoringTask != null) return;

        _cts = new CancellationTokenSource();
        _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        
        _monitoringTask = Task.Run(async () =>
        {
            try
            {
                while (await _periodicTimer.WaitForNextTickAsync(_cts.Token))
                {
                    OnMonitoringTick();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        });
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _monitoringTask = null;
    }

    private void OnMonitoringTick()
    {
        var status = GetSystemStatus();
        StatusUpdated?.Invoke(this, status);
    }

    public IEnumerable<ModuleStatus> GetModuleStatuses()
    {
        var statuses = new List<ModuleStatus>();

        // Check MassBoot (TCP Port 5054)
        bool isServerRunning = IsPortOpen(5054);
        statuses.Add(new ModuleStatus
        {
            Name = "MassBoot Server",
            Icon = "🖥️",
            Status = isServerRunning ? "Running" : "Stopped",
            Color = isServerRunning ? "#10B981" : "#6B7280"
        });

        // Check ProUSB
        // 1. Check if Plugin is loaded via Lifecycle Manager
        var proUsbPlugin = _pluginManager?.LoadedPlugins.Values
            .FirstOrDefault(p => p.Manifest.Id.Equals("prousb", StringComparison.OrdinalIgnoreCase));
        
        bool isProUsbReady = proUsbPlugin?.State == Mass.Core.Plugins.PluginState.Running;

        // 2. If not a plugin, check if ProUSB assembly is loaded in current domain (Core integration)
        if (!isProUsbReady)
        {
             isProUsbReady = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name?.Equals("ProUSB", StringComparison.OrdinalIgnoreCase) == true);
        }

        statuses.Add(new ModuleStatus
        {
            Name = "ProUSB Engine",
            Icon = "💾",
            Status = isProUsbReady ? "Ready" : "Inactive",
            Color = isProUsbReady ? "#10B981" : "#6B7280"
        });

        // Check Orchestrator (Workflows) - assumed ready as part of core
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
        catch (Exception ex)
        {
            _logger?.LogError("Failed to get CPU usage", ex, "StatusService");
        }

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
        catch (Exception ex)
        {
            _logger?.LogError("Failed to get disk info", ex, "StatusService");
        }

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
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize network counters", ex, "StatusService");
        }
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
        catch (Exception ex)
        {
            _logger?.LogError("Failed to get network status", ex, "StatusService");
        }

        return networkStatus;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private double GetCpuUsageWindows()
    {
        return _cpuCounter?.NextValue() ?? 0;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _periodicTimer?.Dispose();
        _cpuCounter?.Dispose();
    }

    private static bool IsPortOpen(int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect("127.0.0.1", port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
            if (!success) return false;
            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
