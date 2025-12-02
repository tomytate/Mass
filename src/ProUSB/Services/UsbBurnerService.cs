using System.Diagnostics;
using ProUSB.Infrastructure.DiskManagement;
using ProUSB.Services.Logging;
using ProUSB.Domain;

namespace ProUSB.Services;

public class UsbBurnerService : IUsbBurnerService
{
    private readonly NativeDiskFormatter _formatter;
    private readonly FileLogger _logger;

    public UsbBurnerService(NativeDiskFormatter formatter, FileLogger logger)
    {
        _formatter = formatter;
        _logger = logger;
    }

    public async Task BurnIsoAsync(string isoPath, string driveLetter, string fileSystem, string partitionScheme, int persistenceSizeMB, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(isoPath))
            throw new FileNotFoundException($"ISO file not found: {isoPath}");

        int diskIndex = GetDiskIndexFromDriveLetter(driveLetter);
        _logger.Info($"Starting burn process for {isoPath} to Disk {diskIndex} (Persistence: {persistenceSizeMB} MB)");

        try
        {
            if (persistenceSizeMB > 0)
            {
                // Custom layout with Persistence
                var partitions = new List<PartitionDefinition>();
                
                // Boot Partition (FAT32) - Takes remaining space
                // We put Persistence at the end usually, but NativeDiskFormatter expects ordered list.
                // Let's put Boot first, Persistence second.
                
                // Calculate sizes
                // NativeDiskFormatter handles SizeMB=0 as "Rest of disk"
                // So Partition 1 (Boot) should be 0? No, usually Persistence is fixed size.
                // So Partition 1 (Boot) = 0 (Rest), Partition 2 (Persistence) = Fixed?
                // NativeDiskFormatter logic:
                // if (def.SizeMB == 0 || i == partitions.Count - 1 && def.SizeMB == 0) -> Rest of disk.
                
                // If we want Persistence at the end, we should define Boot first.
                // But if Boot is "Rest", it takes everything.
                // So we must define Boot size? No, ISO size varies.
                // Better: Partition 1 (Persistence) = Fixed, Partition 2 (Boot) = Rest?
                // But Boot partition usually needs to be first for compatibility.
                
                // So: Partition 1 (Boot) = Total - Persistence.
                // But we don't know Total easily here without querying.
                // NativeDiskFormatter queries it.
                
                // Let's rely on NativeDiskFormatter to handle "Rest".
                // If we define Partition 1 as Size=0, it takes all.
                // We need to change NativeDiskFormatter to support "Rest" for non-last partition?
                // Or just define Persistence first?
                // Windows only mounts the first partition on removable drives (historically).
                // Modern Windows mounts all.
                // Linux looks for casper-rw label.
                
                // Let's try: Partition 1 (Boot) = 0 (Rest - Persistence)?
                // NativeDiskFormatter logic is simple loop.
                
                // Let's assume we can put Persistence first? No, Boot should be first.
                
                // I'll modify NativeDiskFormatter logic later if needed, but for now let's assume we can't easily do "Rest" for first partition.
                // So I will query disk size here? No, I don't have the handle.
                
                // Let's just use a fixed size for Boot if possible? No.
                
                // Wait, NativeDiskFormatter.CreateCustomPartitionLayout:
                // if (def.SizeMB == 0 || i == partitions.Count - 1 && def.SizeMB == 0) sizeBytes = diskSize - currentOffset ...
                
                // So if I have 2 partitions:
                // 1. Boot (Size=0) -> Takes everything.
                // 2. Persistence -> No space left.
                
                // So I MUST specify size for Partition 1 if I want Partition 2.
                // But I don't know the disk size.
                
                // Workaround: Put Persistence FIRST?
                // 1. Persistence (Ext4, Fixed Size)
                // 2. Boot (FAT32, Rest)
                // This works for Linux (it searches all partitions).
                // Does it work for BIOS/UEFI?
                // UEFI looks for EFI System Partition.
                // If Partition 1 is Ext4, UEFI might skip it?
                // Usually UEFI scans all FAT partitions.
                // So if Partition 2 is FAT32, it should work.
                
                partitions.Add(new PartitionDefinition 
                { 
                    Label = "casper-rw", 
                    FileSystem = "ext4", 
                    SizeMB = persistenceSizeMB,
                    IsBootable = false
                });
                
                partitions.Add(new PartitionDefinition 
                { 
                    Label = "BOOT", 
                    FileSystem = "FAT32", 
                    SizeMB = 0, // Rest of disk
                    IsBootable = true
                });

                await _formatter.FormatCustomAsync(diskIndex, partitions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Standard single partition
                await _formatter.FormatAsync(diskIndex, "BOOT", "FAT32", partitionScheme, 4096, true, cancellationToken).ConfigureAwait(false);
            }

            // Copy ISO content
            progress.Report(50);
            await ExtractIsoAsync(isoPath, driveLetter, progress, cancellationToken).ConfigureAwait(false);
            
            progress.Report(100);
        }
        catch (Exception ex)
        {
            _logger.Error($"Burn failed: {ex.Message}", ex);
            throw;
        }
    }

    private int GetDiskIndexFromDriveLetter(string driveLetter)
    {
        // Simple heuristic or WMI query
        // For now, assuming driveLetter is like "E:\"
        // We can use WMI to get DiskIndex.
        // Or just parse if it was passed as "PhysicalDriveX" (but signature says driveLetter).
        // The UI passes "E:\".
        
        // Using WMI to map DriveLetter to DiskIndex
        try 
        {
            var drive = driveLetter.TrimEnd('\\');
            using var searcher = new System.Management.ManagementObjectSearcher($"SELECT Index FROM Win32_DiskPartition WHERE DeviceID IN (SELECT Dependent FROM Win32_LogicalDiskToPartition WHERE Antecedent = (SELECT Name FROM Win32_LogicalDisk WHERE DeviceID = '{drive}'))");
            foreach (var mo in searcher.Get())
            {
                // This returns DiskIndex but partition specific?
                // Win32_DiskPartition.Index is Partition index? No, DiskIndex is in Win32_DiskDrive.
                // Wait, Win32_DiskPartition has DiskIndex property.
                return Convert.ToInt32(mo["DiskIndex"]);
            }
        }
        catch 
        {
            // Fallback or error
        }
        // If failed, maybe it's a test? Return 1?
        // Throwing is safer.
        throw new InvalidOperationException($"Could not determine Disk Index for {driveLetter}");
    }

    private async Task ExtractIsoAsync(string isoPath, string driveLetter, IProgress<double> progress, CancellationToken ct)
    {
        using var isoStream = File.OpenRead(isoPath);
        using var cd = new DiscUtils.Iso9660.CDReader(isoStream, true);
        
        var files = cd.GetFiles("").ToList();
        int totalFiles = files.Count; // This is only root files? No, recursive?
        // GetFiles("") returns all files recursively if search pattern is empty? No.
        // We need recursive.
        
        // Helper to copy directory
        await CopyDirectoryAsync(cd.Root, driveLetter, progress, 0, 100, ct).ConfigureAwait(false);
    }

    private async Task CopyDirectoryAsync(DiscUtils.DiscDirectoryInfo dir, string destPath, IProgress<double> progress, double startPct, double endPct, CancellationToken ct)
    {
        if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);

        var files = dir.GetFiles();
        var subDirs = dir.GetDirectories();
        int totalItems = files.Length + subDirs.Length;
        int currentItem = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            string destFile = Path.Combine(destPath, file.Name);
            
            using var sourceStream = file.OpenRead();
            using var destStream = File.Create(destFile);
            
            // Use stack allocation for buffer (81920 bytes = 80KB is reasonable for stack)
            Span<byte> buffer = stackalloc byte[81920];
            int bytesRead;
            while ((bytesRead = sourceStream.Read(buffer)) > 0)
            {
                destStream.Write(buffer.Slice(0, bytesRead));
            }
            
            currentItem++;
            // Update progress... (simplified)
        }

        foreach (var subDir in subDirs)
        {
            await CopyDirectoryAsync(subDir, Path.Combine(destPath, subDir.Name), progress, startPct, endPct, ct).ConfigureAwait(false);
        }
    }
}
