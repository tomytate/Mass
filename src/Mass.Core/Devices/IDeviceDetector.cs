namespace Mass.Core.Devices;

public interface IDeviceDetector
{
    Task<IEnumerable<IStorageDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);
    
    // C# 14: We can use extension events if we wanted, but standard events are fine.
    event EventHandler<IStorageDevice> DeviceConnected;
    event EventHandler<IStorageDevice> DeviceDisconnected;
    
    void StartMonitoring();
    void StopMonitoring();
}
