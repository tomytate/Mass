using System.Management;
using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Usb;
using Microsoft.Extensions.Logging;
using ProUSB.Adapters;
using ProUSB.Domain;
using ProUSB.Engine;

namespace ProUSB.Services;

public class UsbBurner : IUsbBurner
{
    private readonly BurnEngine _burnEngine;
    private readonly SafetyConfig _safetyConfig;
    private readonly ILogger<UsbBurner> _logger;

    public UsbBurner(
        BurnEngine burnEngine,
        SafetyConfig safetyConfig,
        ILogger<UsbBurner> logger)
    {
        _burnEngine = burnEngine;
        _safetyConfig = safetyConfig;
        _logger = logger;
    }

    public async Task<Mass.Spec.Contracts.Usb.BurnResult> BurnAsync(
        UsbJob job, 
        IProgress<Mass.Spec.Contracts.Usb.BurnProgress>? progress = null, 
        CancellationToken ct = default)
    {
        if (!_safetyConfig.AllowRealWrites)
        {
            throw new OperationException("elevation_required", 
                "Real hardware writes are disabled. Set SafetyConfig.AllowRealWrites = true to enable.");
        }

        _logger.LogInformation("Starting burn operation for {ImagePath} to {DeviceId}", 
            job.ImagePath, job.TargetDeviceId);

        var startTime = DateTime.UtcNow;
        
        try
        {
            var legacyRequest = new ProUSB.Engine.BurnRequest(
                IsoPath: job.ImagePath,
                TargetDeviceId: job.TargetDeviceId,
                PartitionScheme: job.PartitionScheme,
                FileSystem: job.FileSystem,
                PersistenceSizeMB: job.PersistenceSizeMB
            );

            IProgress<ProUSB.Engine.BurnProgress>? legacyProgress = null;
            if (progress != null)
            {
                legacyProgress = new Progress<ProUSB.Engine.BurnProgress>(legacyProg =>
                {
                    var massSpecProgress = new Mass.Spec.Contracts.Usb.BurnProgress
                    {
                        Percentage = legacyProg.Percentage,
                        CurrentOperation = legacyProg.CurrentOperation,
                        BytesProcessed = legacyProg.BytesProcessed,
                        TotalBytes = legacyProg.TotalBytes
                    };
                    progress.Report(massSpecProgress);
                });
            }

            var legacyResult = await _burnEngine.BurnIsoAsync(
                legacyRequest, 
                legacyProgress!, 
                ct);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Burn operation completed in {Duration}", duration);

            return new Mass.Spec.Contracts.Usb.BurnResult
            {
                IsSuccess = legacyResult.Success,
                ErrorMessage = legacyResult.Success ? null : legacyResult.Message,
                Duration = legacyResult.Duration
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Burn operation was cancelled");
            return new Mass.Spec.Contracts.Usb.BurnResult
            {
                IsSuccess = false,
                ErrorMessage = "Operation cancelled by user",
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Burn operation failed");
            return new Mass.Spec.Contracts.Usb.BurnResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<IEnumerable<DeviceInfo>> ListDevicesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Enumerating USB devices");
            
            var devices = new List<DeviceInfo>();
            
            if (!OperatingSystem.IsWindows())
            {
                return devices;
            }

            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                
                foreach (ManagementObject disk in searcher.Get())
                {
                    var deviceId = disk["DeviceID"]?.ToString() ?? "";
                    var model = disk["Model"]?.ToString() ?? "Unknown";
                    var size = Convert.ToInt64(disk["Size"] ?? 0);
                    
                    devices.Add(new DeviceInfo
                    {
                        Id = deviceId,
                        Name = model,
                        Size = size,
                        Path = deviceId,
                        IsRemovable = true
                    });
                }
            }, ct);

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate USB devices");
            return [];
        }
    }

    public async Task<Mass.Spec.Contracts.Usb.VerifyResult> VerifyAsync(
        UsbJob job, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Verifying device {DeviceId}", job.TargetDeviceId);

            var legacyResult = await _burnEngine.VerifyAsync(job.TargetDeviceId, ct);

            return new Mass.Spec.Contracts.Usb.VerifyResult
            {
                IsSuccess = legacyResult.Success,
                Errors = legacyResult.Errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification failed for device {DeviceId}", job.TargetDeviceId);
            return new Mass.Spec.Contracts.Usb.VerifyResult
            {
                IsSuccess = false,
                Errors = [ex.Message]
            };
        }
    }
}
