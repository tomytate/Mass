namespace ProUSB.Drivers;

/// <summary>
/// Dummy driver adapter for testing without real hardware.
/// </summary>
public class DummyDriverAdapter : IDriverAdapter
{
    public string Name => "Dummy";
    public bool IsAvailable => false;

    public Task<bool> FormatDiskAsync(int diskIndex, string label, string fileSystem, string partitionScheme, int clusterSize, bool quickFormat, CancellationToken ct)
    {
        return Task.FromResult(false);
    }

    public Task<bool> FormatCustomAsync(int diskIndex, List<ProUSB.Domain.PartitionDefinition> partitions, CancellationToken ct)
    {
        return Task.FromResult(false);
    }

    public Task<string?> MountPartitionAsync(int diskIndex, int partitionIndex, CancellationToken ct)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<bool> UnmountDiskAsync(int diskIndex, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    public Task<DiskInfo> GetDiskInfoAsync(int diskIndex, CancellationToken ct)
    {
        return Task.FromResult(new DiskInfo(diskIndex, 0, "Dummy", false, new List<PartitionInfo>()));
    }
}
