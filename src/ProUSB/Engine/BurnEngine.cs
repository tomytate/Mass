using ProUSB.Drivers;
using ProUSB.Services.Logging;
using DiscUtils.Iso9660;
using System.Management;

namespace ProUSB.Engine;

public class BurnEngine
{
    private readonly IDriverAdapter _driver;
    private readonly FileLogger _logger;
    private CancellationTokenSource? _cts;

    public BurnEngine(IDriverAdapter driver, FileLogger logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public async Task<BurnResult> BurnIsoAsync(
        BurnRequest request, 
        IProgress<BurnProgress> progress, 
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            _logger.Info($"Starting burn: {request.IsoPath} -> {request.TargetDeviceId}");
            
            if (!File.Exists(request.IsoPath))
                throw new FileNotFoundException($"ISO file not found: {request.IsoPath}");

            progress.Report(new BurnProgress(0, "Formatting disk...", 0, 100));
            
            var diskIndex = ParseDiskIndex(request.TargetDeviceId);
            bool formatSuccess;

            if (request.PersistenceSizeMB > 0)
            {
                var partitions = new List<ProUSB.Domain.PartitionDefinition>
                {
                    new() { 
                        Label = "casper-rw", 
                        FileSystem = "ext4", 
                        SizeMB = request.PersistenceSizeMB,
                        IsBootable = false
                    },
                    new() { 
                        Label = "BOOT", 
                        FileSystem = request.FileSystem, 
                        SizeMB = 0,
                        IsBootable = true
                    }
                };
                
                formatSuccess = await _driver.FormatCustomAsync(diskIndex, partitions, _cts.Token);
            }
            else
            {
                formatSuccess = await _driver.FormatDiskAsync(
                    diskIndex,
                    "BOOT",
                    request.FileSystem,
                    request.PartitionScheme,
                    4096,
                    true,
                    _cts.Token);
            }

            if (!formatSuccess)
                throw new InvalidOperationException("Disk formatting failed");

            progress.Report(new BurnProgress(40, "Format complete. Copying files...", 40, 100));

            var mountPoint = await _driver.MountPartitionAsync(diskIndex, 0, _cts.Token) 
                ?? throw new InvalidOperationException("Failed to mount formatted disk");

            await ExtractIsoAsync(request.IsoPath, mountPoint, progress, _cts.Token);

            progress.Report(new BurnProgress(100, "Burn complete!", 100, 100));

            var duration = DateTime.UtcNow - startTime;
            _logger.Info($"Burn completed successfully in {duration.TotalSeconds:F1}s");

            return new BurnResult(true, "Burn successful", duration);
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Burn cancelled by user");
            return new BurnResult(false, "Cancelled", DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.Error($"Burn failed: {ex.Message}", ex);
            return new BurnResult(false, ex.Message, DateTime.UtcNow - startTime);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task<VerifyResult> VerifyAsync(string devicePath, CancellationToken ct = default)
    {
        var errors = new List<string>();
        
        try
        {
            if (!Directory.Exists(devicePath))
            {
                errors.Add($"Device path not found: {devicePath}");
                return new VerifyResult(false, "Device not accessible", errors);
            }

            var files = Directory.GetFiles(devicePath, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                errors.Add("No files found on device");
                return new VerifyResult(false, "Empty device", errors);
            }

            await Task.CompletedTask;
            return new VerifyResult(true, $"Verified {files.Length} files", errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return new VerifyResult(false, "Verification failed", errors);
        }
    }

    public async Task<bool> CancelAsync()
    {
        _cts?.Cancel();
        await Task.CompletedTask;
        return true;
    }

    private int ParseDiskIndex(string deviceId)
    {
        if (deviceId.Contains("PhysicalDrive", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(deviceId, @"\d+");
            if (match.Success) return int.Parse(match.Value);
        }
        
        if (deviceId.Length == 2 && deviceId[1] == ':')
        {
            return MapDriveLetterToDiskIndex(deviceId[0]);
        }
        
        throw new ArgumentException($"Invalid device ID format: {deviceId}");
    }

    private int MapDriveLetterToDiskIndex(char driveLetter)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '{driveLetter}:'");
        
        foreach (ManagementObject disk in searcher.Get())
        {
            var deviceId = disk["DeviceID"]?.ToString();
            if (deviceId != null)
            {
                using var partitionSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{deviceId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                
                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    using var diskSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                    
                    foreach (ManagementObject drive in diskSearcher.Get())
                    {
                        var driveDeviceId = drive["DeviceID"]?.ToString();
                        if (driveDeviceId != null)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(driveDeviceId, @"\d+");
                            if (match.Success) return int.Parse(match.Value);
                        }
                    }
                }
            }
        }
        
        throw new InvalidOperationException($"Could not map drive {driveLetter}: to disk index");
    }

    private async Task ExtractIsoAsync(
        string isoPath, 
        string targetPath, 
        IProgress<BurnProgress> progress, 
        CancellationToken ct)
    {
        using var isoStream = File.OpenRead(isoPath);
        using var cd = new CDReader(isoStream, true);

        await CopyDirectoryAsync(cd.Root, targetPath, progress, 40, 95, ct);
    }

    private async Task CopyDirectoryAsync(
        DiscUtils.DiscDirectoryInfo sourceDir, 
        string destPath, 
        IProgress<BurnProgress> progress, 
        int startPct, 
        int endPct, 
        CancellationToken ct)
    {
        if (!Directory.Exists(destPath)) 
            Directory.CreateDirectory(destPath);

        var files = sourceDir.GetFiles();
        var subDirs = sourceDir.GetDirectories();
        int totalItems = files.Length + subDirs.Length;
        int currentItem = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            
            string destFile = Path.Combine(destPath, file.Name);
            
            using var sourceStream = file.OpenRead();
            using var destStream = File.Create(destFile);
            
            Span<byte> buffer = stackalloc byte[81920];
            int bytesRead;
            while ((bytesRead = sourceStream.Read(buffer)) > 0)
            {
                destStream.Write(buffer[..bytesRead]);
            }
            
            currentItem++;
            var pct = startPct + (int)((currentItem / (double)totalItems) * (endPct - startPct));
            progress.Report(new BurnProgress(pct, $"Copying {file.Name}...", currentItem, totalItems));
        }

        foreach (var subDir in subDirs)
        {
            await CopyDirectoryAsync(
                subDir, 
                Path.Combine(destPath, subDir.Name), 
                progress, 
                startPct, 
                endPct, 
                ct);
        }
    }
}
