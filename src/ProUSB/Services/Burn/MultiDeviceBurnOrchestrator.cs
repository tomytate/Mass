using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Services.Logging;

namespace ProUSB.Services.Burn;

public class MultiDeviceBurnOrchestrator {
    private readonly ParallelBurnService _burnService;
    private readonly FileLogger _logger;
    private const int MaxConcurrentBurns = 3;
    
    public MultiDeviceBurnOrchestrator(ParallelBurnService burnService, FileLogger logger) {
        _burnService = burnService;
        _logger = logger;
    }
    
    public async Task BurnMultipleAsync(
        List<UsbDeviceInfo> devices,
        string isoPath,
        DeploymentConfiguration baseConfig,
        Action<string, double, string> progressCallback,
        CancellationToken ct
    ) {
        if(devices.Count == 0) {
            throw new ArgumentException("No devices provided", nameof(devices));
        }
        
        _logger.Info($"Starting multi-device burn for {devices.Count} device(s)");
        
        var results = new ConcurrentBag<BurnResult>();
        var semaphore = new SemaphoreSlim(MaxConcurrentBurns);
        
        var tasks = devices.Select(async device => {
            await semaphore.WaitAsync(ct);
            try {
                _logger.Info($"Starting burn for {device.FriendlyName}");
                progressCallback(device.DeviceId, 0, "Starting...");
                
                var config = new DeploymentConfiguration {
                    JobName = $"Burn-{device.DeviceId}",
                    TargetDevice = device,
                    SourceIso = baseConfig.SourceIso,
                    Strategy = baseConfig.Strategy,
                    PartitionScheme = baseConfig.PartitionScheme,
                    FileSystem = baseConfig.FileSystem,
                    ClusterSize = baseConfig.ClusterSize,
                    QuickFormat = baseConfig.QuickFormat,
                    BypassWin11 = baseConfig.BypassWin11,
                    PersistenceSize = baseConfig.PersistenceSize
                };
                
                var progress = new Progress<WriteStatistics>(stats => {
                    progressCallback(device.DeviceId, stats.PercentComplete, stats.Message);
                });
                
                await _burnService.BurnAsync(config, progress, ct);
                
                results.Add(new BurnResult {
                    Device = device,
                    Success = true,
                    Message = "Completed successfully"
                });
                
                progressCallback(device.DeviceId, 100, "✅ Complete");
                _logger.Info($"Burn completed successfully for {device.FriendlyName}");
            }
            catch(OperationCanceledException) {
                results.Add(new BurnResult {
                    Device = device,
                    Success = false,
                    Message = "Cancelled by user"
                });
                progressCallback(device.DeviceId, 0, "⚠️ Cancelled");
                _logger.Warn($"Burn cancelled for {device.FriendlyName}");
            }
            catch(Exception ex) {
                results.Add(new BurnResult {
                    Device = device,
                    Success = false,
                    Message = ex.Message
                });
                
                progressCallback(device.DeviceId, 0, $"❌ Failed");
                _logger.Error($"Burn failed for {device.FriendlyName}: {ex.Message}");
            }
            finally {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        
        LogSummary(results.ToList());
    }
    
    public async Task BurnBatchAsync(
        List<BurnJob> jobs,
        Action<string, double, string> progressCallback,
        CancellationToken ct
    ) {
        if(jobs.Count == 0) throw new ArgumentException("No jobs provided", nameof(jobs));

        _logger.Info($"Starting batch burn for {jobs.Count} job(s)");
        
        var results = new ConcurrentBag<BurnResult>();
        var semaphore = new SemaphoreSlim(MaxConcurrentBurns);

        var tasks = jobs.Select(async job => {
            await semaphore.WaitAsync(ct);
            try {
                _logger.Info($"Starting batch job for {job.Device.FriendlyName} with ISO {Path.GetFileName(job.IsoPath)}");
                progressCallback(job.Device.DeviceId, 0, "Starting...");

                var progress = new Progress<WriteStatistics>(stats => {
                    progressCallback(job.Device.DeviceId, stats.PercentComplete, stats.Message);
                });

                await _burnService.BurnAsync(job.Config, progress, ct);

                results.Add(new BurnResult {
                    Device = job.Device,
                    Success = true,
                    Message = "Completed successfully"
                });

                progressCallback(job.Device.DeviceId, 100, "✅ Complete");
                _logger.Info($"Batch job completed for {job.Device.FriendlyName}");
            }
            catch(OperationCanceledException) {
                results.Add(new BurnResult {
                    Device = job.Device,
                    Success = false,
                    Message = "Cancelled by user"
                });
                progressCallback(job.Device.DeviceId, 0, "⚠️ Cancelled");
            }
            catch(Exception ex) {
                results.Add(new BurnResult {
                    Device = job.Device,
                    Success = false,
                    Message = ex.Message
                });
                progressCallback(job.Device.DeviceId, 0, "❌ Failed");
                _logger.Error($"Batch job failed for {job.Device.FriendlyName}: {ex.Message}");
            }
            finally {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        LogSummary(results.ToList());
    }

    private void LogSummary(List<BurnResult> results) {
        var successful = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        
        _logger.Info($"=== Burn Summary ===");
        _logger.Info($"Total: {results.Count} | Success: {successful} | Failed: {failed}");
        
        if(failed > 0) {
            _logger.Info("Failed devices:");
            foreach(var result in results.Where(r => !r.Success)) {
                _logger.Error($"  • {result.Device.FriendlyName}: {result.Message}");
            }
        }
    }
    
    public record BurnResult {
        public required UsbDeviceInfo Device { get; init; }
        public required bool Success { get; init; }
        public required string Message { get; init; }
    }
}


