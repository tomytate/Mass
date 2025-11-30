using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ProUSB.Domain;
using ProUSB.Services.Logging;

namespace ProUSB.Services.IsoCreation;

public class IsoCreationService {
    private readonly FileLogger _logger;
    
    public IsoCreationService(FileLogger logger) {
        _logger = logger;
    }
    
    public async Task<IsoCreationResult> CreateIsoFromDeviceAsync(
        UsbDeviceInfo device,
        string outputPath,
        IsoCreationMode mode,
        IProgress<IsoCreationProgress> progress,
        CancellationToken ct
    ) {
        _logger.Info($"Creating ISO from {device.FriendlyName}");
        _logger.Info($"Output: {outputPath}");
        _logger.Info($"Mode: {mode}");
        _logger.Info($"Device size: {FormatBytes(device.TotalSize)}");
        
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
        
        _logger.Info($"Opening physical device: {physicalPath}");
        
        var handle = CreateFile(
            physicalPath,
            FileAccess.Read,
            FileShare.ReadWrite,
            IntPtr.Zero,
            FileMode.Open,
            0,
            IntPtr.Zero
        );
        
        if(handle.IsInvalid) {
            var error = $"Cannot open device {physicalPath}";
            _logger.Error(error);
            return new IsoCreationResult {
                Success = false,
                ErrorMessage = error
            };
        }
        
        try {
            using var source = new FileStream(handle, FileAccess.Read);
            using var dest = File.Create(outputPath);
            
            var buffer = new byte[1024 * 1024];
            int bytesRead;
            var lastUpdate = DateTime.Now;
            
            _logger.Info("Starting raw sector copy...");
            
            while((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0) {
                await dest.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                copiedBytes += bytesRead;
                
                var now = DateTime.Now;
                if((now - lastUpdate).TotalMilliseconds >= 500 || copiedBytes == totalBytes) {
                    var percent = (double)copiedBytes / totalBytes * 100;
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
            _logger.Info($"ISO creation completed in {duration.TotalSeconds:F1}s");
            _logger.Info($"Total size: {FormatBytes(copiedBytes)}");
            
            return new IsoCreationResult {
                Success = true,
                OutputPath = outputPath,
                FileSizeBytes = copiedBytes,
                CreationMode = IsoCreationMode.RawCopy,
                Duration = duration
            };
        }
        catch(Exception ex) {
            _logger.Error($"ISO creation failed: {ex.Message}");
            
            if(File.Exists(outputPath)) {
                try {
                    File.Delete(outputPath);
                    _logger.Info("Deleted incomplete ISO file");
                }
                catch {
                    _logger.Warn("Could not delete incomplete ISO file");
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
    
    private string FormatBytes(long bytes) {
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


