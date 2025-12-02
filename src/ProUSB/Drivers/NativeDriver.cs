using ProUSB.Domain;
using ProUSB.Infrastructure.DiskManagement;

namespace ProUSB.Drivers;

public class NativeDriver : IDriverAdapter
{
    private readonly NativeDiskFormatter _formatter;

    public string Name => "Native Win32 API";
    public bool IsAvailable => OperatingSystem.IsWindows();

    public NativeDriver(NativeDiskFormatter formatter)
    {
        _formatter = formatter;
    }

    public async Task<bool> FormatDiskAsync(
        int diskIndex, 
        string label, 
        string fileSystem, 
        string partitionScheme, 
        int allocationUnitSize, 
        bool markActive, 
        CancellationToken ct = default)
    {
        try
        {
            await _formatter.FormatAsync(
                diskIndex, 
                label, 
                fileSystem, 
                partitionScheme, 
                allocationUnitSize, 
                markActive, 
                ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> FormatCustomAsync(
        int diskIndex, 
        List<PartitionDefinition> partitions, 
        CancellationToken ct = default)
    {
        try
        {
            await _formatter.FormatCustomAsync(diskIndex, partitions, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> MountPartitionAsync(int diskIndex, int partitionIndex, CancellationToken ct = default)
    {
        return await Task.Run(() => 
        {
            try 
            {
                var query = $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='\\\\\\\\.\\\\PHYSICALDRIVE{diskIndex}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                using var searcher = new System.Management.ManagementObjectSearcher(query);
                
                int currentPart = 0;
                foreach (System.Management.ManagementObject partition in searcher.Get())
                {
                    if (currentPart == partitionIndex)
                    {
                        var logicalSearcher = new System.Management.ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                        
                        foreach (System.Management.ManagementObject logical in logicalSearcher.Get())
                        {
                            return logical["DeviceID"]?.ToString();
                        }
                    }
                    currentPart++;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }, ct);
    }

    public async Task<bool> UnmountDiskAsync(int diskIndex, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return false;
    }

    public async Task<DiskInfo> GetDiskInfoAsync(int diskIndex, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM Win32_DiskDrive WHERE Index = {diskIndex}");
                
                foreach (System.Management.ManagementObject drive in searcher.Get())
                {
                    var model = drive["Model"]?.ToString() ?? "Unknown";
                    var size = Convert.ToInt64(drive["Size"]);
                    var mediaType = drive["MediaType"]?.ToString() ?? "Unknown";
                    var isRemovable = mediaType.Contains("Removable", StringComparison.OrdinalIgnoreCase) || 
                                      drive["InterfaceType"]?.ToString() == "USB";

                    return new DiskInfo(diskIndex, size, model, isRemovable, new List<PartitionInfo>());
                }
            }
            catch { }
            
            return new DiskInfo(diskIndex, 0, "Unknown", false, new List<PartitionInfo>());
        }, ct);
    }
}
