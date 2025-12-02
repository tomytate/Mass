using System.Management;
using System.Runtime.Versioning;

namespace Mass.Core.Devices;

[SupportedOSPlatform("windows")]
public class WindowsDeviceDetector : IDeviceDetector
{
    private ManagementEventWatcher? _insertWatcher;
    private ManagementEventWatcher? _removeWatcher;

    public event EventHandler<IStorageDevice>? DeviceConnected;
    public event EventHandler<IStorageDevice>? DeviceDisconnected;

    public async Task<IEnumerable<IStorageDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => 
        {
            var devices = new List<IStorageDevice>();
            
            // Query for Disk Drives
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            foreach (ManagementObject drive in searcher.Get())
            {
                var device = ParseDevice(drive);
                if (device != null)
                {
                    devices.Add(device);
                }
            }
            return devices;
        }, cancellationToken);
    }

    public void StartMonitoring()
    {
        if (_insertWatcher != null) return;

        var insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive' AND TargetInstance.InterfaceType='USB'");
        _insertWatcher = new ManagementEventWatcher(insertQuery);
        _insertWatcher.EventArrived += (s, e) => 
        {
            var target = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var device = ParseDevice(target);
            if (device != null)
            {
                DeviceConnected?.Invoke(this, device);
            }
        };
        _insertWatcher.Start();

        var removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive' AND TargetInstance.InterfaceType='USB'");
        _removeWatcher = new ManagementEventWatcher(removeQuery);
        _removeWatcher.EventArrived += (s, e) => 
        {
            var target = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var device = ParseDevice(target);
            if (device != null)
            {
                DeviceDisconnected?.Invoke(this, device);
            }
        };
        _removeWatcher.Start();
    }

    public void StopMonitoring()
    {
        _insertWatcher?.Stop();
        _insertWatcher?.Dispose();
        _insertWatcher = null;

        _removeWatcher?.Stop();
        _removeWatcher?.Dispose();
        _removeWatcher = null;
    }

    private IStorageDevice? ParseDevice(ManagementBaseObject drive)
    {
        try
        {
            var deviceId = drive["DeviceID"]?.ToString() ?? "";
            var model = drive["Model"]?.ToString() ?? "Unknown";
            var size = Convert.ToInt64(drive["Size"]);
            var pnpDeviceId = drive["PNPDeviceID"]?.ToString() ?? "";
            
            // Extract VID/PID from PNPDeviceID if possible for signature matching
            // Example: USB\VID_0951&PID_1666\00000000000000
            
            // For now, we return a basic implementation
            return new WindowsStorageDevice
            {
                Id = deviceId,
                Model = model,
                Path = deviceId,
                SizeBytes = size,
                IsRemovable = true, // Assumed for USB query
                BusType = "USB"
            };
        }
        catch
        {
            return null;
        }
    }
}

public class WindowsStorageDevice : IStorageDevice
{
    public required string Id { get; init; }
    public required string Model { get; init; }
    public required string Path { get; init; }
    public long SizeBytes { get; init; }
    public bool IsRemovable { get; init; }
    public required string BusType { get; init; }

    public Task EjectAsync()
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsLockedAsync()
    {
        return Task.FromResult(false);
    }
}
