using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ProUSB.Domain;
using Microsoft.Extensions.Logging;

namespace ProUSB.Services.IsoCreation;

public class IsoCreationService : IIsoCreationService {
    private readonly ILogger<IsoCreationService> _logger;
    
    public IsoCreationService(ILogger<IsoCreationService> logger) {
        _logger = logger;
    }
    
    public async Task<IsoCreationResult> CreateIsoFromDeviceAsync(
        UsbDeviceInfo device,
        string outputPath,
        IsoCreationMode mode,
        IProgress<IsoCreationProgress> progress,
        CancellationToken ct
    ) {
        _logger.LogInformation("Creating ISO from {DeviceName}. Output: {Output}. Mode: {Mode}",
            device.FriendlyName, outputPath, mode);
        
        return mode switch {
            IsoCreationMode.RawCopy => await CreateRawIsoAsync(device, outputPath, progress, ct),
            _ => throw new ArgumentException($"Unsupported creation mode: {mode}")
        };
    }
    
    private async Task<IsoCreationResult> CreateRawIsoAsync(
        UsbDeviceInfo device,
        string outputPath,
        IProgress<IsoCreationProgress> progress,
        CancellationToken ct
    ) {
        var startTime = DateTime.Now;
        long totalBytes = device.TotalSize;
        long copiedBytes = 0;
        
        var physicalPath = $"\\\\.\\PhysicalDrive{device.PhysicalIndex}";
        
        _logger.LogInformation("Opening physical device: {PhysicalPath}", physicalPath);
        
        // This is a Windows-specific P/Invoke.
        // For strict cross-platform, this logic needs to be abstracted behind a platform-specific adapter.
        // For now, wrapping in try/catch and platform check.

        if (!OperatingSystem.IsWindows())
        {
             return new IsoCreationResult {
                Success = false,
                ErrorMessage = "Raw copy mode is currently only supported on Windows."
            };
        }

        SafeFileHandle handle;
        try
        {
            handle = CreateFile(
                physicalPath,
                FileAccess.Read,
                FileShare.ReadWrite,
                IntPtr.Zero,
                FileMode.Open,
                0,
                IntPtr.Zero
            );
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Native error opening device handle");
             return new IsoCreationResult {
                Success = false,
                ErrorMessage = $"Native error opening device: {ex.Message}"
            };
        }
        
        if(handle.IsInvalid) {
            var error = $"Cannot open device {physicalPath}. Ensure you have Administrator privileges.";
            _logger.LogError(error);
            return new IsoCreationResult {
                Success = false,
                ErrorMessage = error
            };
        }
        
        try {
            using var source = new FileStream(handle, FileAccess.Read);
            using var dest = File.Create(outputPath);
            
            var buffer = new byte[1024 * 1024]; // 1MB buffer
            int bytesRead;
            var lastUpdate = DateTime.Now;
            
            _logger.LogInformation("Starting raw sector copy...");
            
            while((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0) {
                await dest.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                copiedBytes += bytesRead;
                
                var now = DateTime.Now;
                if((now - lastUpdate).TotalMilliseconds >= 500 || copiedBytes == totalBytes) {
                    var percent = totalBytes > 0 ? (double)copiedBytes / totalBytes * 100 : 0;
                    var elapsed = now - startTime;
                    var speed = elapsed.TotalSeconds > 0 ? copiedBytes / elapsed.TotalSeconds : 0;
                    
                    progress?.Report(new IsoCreationProgress {
                        PercentComplete = percent,
                        BytesCopied = copiedBytes,
                        TotalBytes = totalBytes,
                        SpeedBytesPerSecond = speed,
                        Message = $"Copying... {FormatBytes(copiedBytes)}/{FormatBytes(totalBytes)} @ {FormatBytes((long)speed)}/s"
                    });
                    
                    lastUpdate = now;
                }
            }
            
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("ISO creation completed in {Duration:F1}s. Total size: {Size}", duration.TotalSeconds, FormatBytes(copiedBytes));
            
            return new IsoCreationResult {
                Success = true,
                OutputPath = outputPath,
                FileSizeBytes = copiedBytes,
                CreationMode = IsoCreationMode.RawCopy,
                Duration = duration
            };
        }
        catch(Exception ex) {
            _logger.LogError(ex, "ISO creation failed");
            
            if(File.Exists(outputPath)) {
                try {
                    File.Delete(outputPath);
                    _logger.LogInformation("Deleted incomplete ISO file");
                }
                catch (Exception cleanupEx) {
                    _logger.LogWarning(cleanupEx, "Could not delete incomplete ISO file");
                }
            }
            
            return new IsoCreationResult {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally {
            handle.Close();
        }
    }
    
    private static string FormatBytes(long bytes) {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        FileAccess dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        FileMode dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );
}
