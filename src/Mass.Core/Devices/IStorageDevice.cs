namespace Mass.Core.Devices;

public interface IStorageDevice
{
    string Id { get; }
    string Model { get; }
    string Path { get; }
    long SizeBytes { get; }
    bool IsRemovable { get; }
    string BusType { get; } // USB, NVMe, SATA
    
    Task<bool> IsLockedAsync();
    Task EjectAsync();
}
