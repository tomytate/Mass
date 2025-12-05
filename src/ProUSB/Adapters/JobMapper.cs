using Mass.Spec.Contracts.Usb;
using ProUSB.Domain;

namespace ProUSB.Adapters;

/// <summary>
/// Maps between Mass.Spec contracts and ProUSB internal models.
/// </summary>
public static class JobMapper
{
    /// <summary>
    /// Converts Mass.Spec UsbJob to ProUSB internal DeploymentConfiguration and metadata.
    /// </summary>
    public static (IsoMetadata iso, UsbDeviceInfo device, DeploymentConfiguration config) ToInternalJob(UsbJob job)
    {
        var iso = new IsoMetadata
        {
            FilePath = job.ImagePath,
            FileName = Path.GetFileName(job.ImagePath),
            FileSize = File.Exists(job.ImagePath) ? new FileInfo(job.ImagePath).Length : 0
        };

        var device = new UsbDeviceInfo
        {
            DeviceId = job.TargetDeviceId,
            FriendlyName = job.TargetDeviceId,
            PhysicalIndex = ParsePhysicalIndex(job.TargetDeviceId)
        };

        var config = new DeploymentConfiguration
        {
            JobName = $"Burn_{DateTime.Now:yyyyMMdd_HHmmss}",
            SourceIso = iso,
            TargetDevice = device,
            Strategy = BurnStrategy.FileSystemCopy,
            VolumeLabel = job.VolumeLabel,
            FileSystem = job.FileSystem.ToLowerInvariant(),
            PartitionScheme = job.PartitionScheme.ToLowerInvariant(),
            PersistenceSize = job.PersistenceSizeMB
        };

        return (iso, device, config);
    }

    /// <summary>
    /// Converts ProUSB UsbDeviceInfo to Mass.Spec DeviceInfo.
    /// </summary>
    public static DeviceInfo ToDeviceInfo(UsbDeviceInfo device)
    {
        return new DeviceInfo
        {
            Id = device.DeviceId,
            Name = device.FriendlyName,
            Size = device.TotalSize,
            IsRemovable = true
        };
    }

    /// <summary>
    /// Converts ProUSB WriteStatistics to Mass.Spec BurnProgress.
    /// </summary>
    public static BurnProgress ToBurnProgress(WriteStatistics stats)
    {
        return new BurnProgress
        {
            Percentage = stats.PercentComplete,
            CurrentOperation = stats.Message,
            BytesProcessed = stats.BytesWritten,
            TotalBytes = stats.BytesWritten > 0 && stats.PercentComplete > 0 
                ? (long)(stats.BytesWritten / (stats.PercentComplete / 100.0)) 
                : 0,
            SpeedBytesPerSecond = stats.SpeedMBps * 1024 * 1024,
            Eta = stats.TimeRemaining
        };
    }

    /// <summary>
    /// Creates a Mass.Spec BurnResult from success/failure information.
    /// </summary>
    public static BurnResult ToBurnResult(bool success, string message, TimeSpan duration)
    {
        return new BurnResult
        {
            IsSuccess = success,
            ErrorMessage = success ? null : message,
            Duration = duration
        };
    }

    /// <summary>
    /// Creates a Mass.Spec VerifyResult from ProUSB verification data.
    /// </summary>
    public static VerifyResult ToVerifyResult(bool success, List<string> errors)
    {
        return new VerifyResult
        {
            IsSuccess = success,
            Errors = errors
        };
    }

    private static int ParsePhysicalIndex(string deviceId)
    {
        // Try to extract physical disk index from device ID
        // Format might be "PhysicalDrive1" or similar
        if (deviceId.StartsWith("PhysicalDrive", StringComparison.OrdinalIgnoreCase))
        {
            var indexStr = deviceId.Substring("PhysicalDrive".Length);
            if (int.TryParse(indexStr, out var index))
                return index;
        }
        
        return 0;
    }
}
