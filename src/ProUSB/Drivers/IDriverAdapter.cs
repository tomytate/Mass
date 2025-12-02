using ProUSB.Domain;

namespace ProUSB.Drivers;

public interface IDriverAdapter
{
    string Name { get; }
    bool IsAvailable { get; }
    
    Task<bool> FormatDiskAsync(
        int diskIndex, 
        string label, 
        string fileSystem, 
        string partitionScheme, 
        int allocationUnitSize,
        bool markActive,
        CancellationToken ct = default);
    
    Task<bool> FormatCustomAsync(
        int diskIndex,
        List<PartitionDefinition> partitions,
        CancellationToken ct = default);
    
    Task<string?> MountPartitionAsync(int diskIndex, int partitionIndex, CancellationToken ct = default);
    
    Task<bool> UnmountDiskAsync(int diskIndex, CancellationToken ct = default);
    
    Task<DiskInfo> GetDiskInfoAsync(int diskIndex, CancellationToken ct = default);
}

public record DiskInfo(
    int DiskIndex,
    long SizeBytes,
    string Model,
    bool IsRemovable,
    List<PartitionInfo> Partitions
);

public record PartitionInfo(
    int Index,
    long SizeBytes,
    string FileSystem,
    string? MountPoint,
    bool IsActive
);
