using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ProUSB.Domain;
using ProUSB.Services.Logging;

namespace ProUSB.Services.Verification;

public class BootVerificationService {
    private readonly FileLogger _logger;
    
    public BootVerificationService(FileLogger logger) {
        _logger = logger;
    }
    
    public async Task<BootVerificationResult> VerifyDeviceAsync(UsbDeviceInfo device, CancellationToken ct) {
        _logger.Info($"Starting bootability verification for {device.FriendlyName}");
        
        var result = new BootVerificationResult {
            DeviceName = device.FriendlyName,
            DeviceId = device.DeviceId
        };
        
        try {
            var physicalPath = $"\\\\.\\PhysicalDrive{device.PhysicalIndex}";
            
            var bootSector = await ReadBootSectorAsync(physicalPath, ct);
            
            result.HasValidMBRSignature = CheckMBRSignature(bootSector);
            result.Details.Add($"MBR Signature: {(result.HasValidMBRSignature ? "Valid (0x55AA)" : "Invalid")}");
            
            result.HasActivePartition = CheckActivePartitionFlag(bootSector);
            result.Details.Add($"Active Partition: {(result.HasActivePartition ? "Present" : "None")}");
            
            result.IsGPT = await CheckGPTSignatureAsync(physicalPath, ct);
            result.Details.Add($"Partition Style: {(result.IsGPT ? "GPT" : "MBR")}");
            
            if(result.IsGPT) {
                result.HasESP = CheckESPPartition(device);
                if(result.HasESP) {
                    result.Details.Add("EFI System Partition: Found");
                }
            }
            
            result.DetectedBootloader = await DetectBootloaderAsync(device, ct);
            result.Details.Add($"Bootloader: {result.DetectedBootloader}");
            
            DetermineBootability(result);
            
            _logger.Info($"Verification complete: {result.GetSummary()}");
        }
        catch(Exception ex) {
            _logger.Error($"Verification failed: {ex.Message}");
            result.Warnings.Add($"Verification error: {ex.Message}");
        }
        
        return result;
    }
    
    private bool CheckMBRSignature(byte[] bootSector) {
        if(bootSector == null || bootSector.Length < 512) return false;
        return bootSector[510] == 0x55 && bootSector[511] == 0xAA;
    }
    
    private bool CheckActivePartitionFlag(byte[] bootSector) {
        if(bootSector == null || bootSector.Length < 512) return false;
        
        for(int i = 0; i < 4; i++) {
            int offset = 446 + (i * 16);
            if(bootSector[offset] == 0x80) {
                return true;
            }
        }
        return false;
    }
    
    private async Task<bool> CheckGPTSignatureAsync(string physicalPath, CancellationToken ct) {
        try {
            var gptHeader = await ReadSectorAsync(physicalPath, 1, ct);
            if(gptHeader.Length >= 8) {
                var signature = Encoding.ASCII.GetString(gptHeader, 0, 8);
                return signature == "EFI PART";
            }
        }
        catch {
            return false;
        }
        return false;
    }
    
    private bool CheckESPPartition(UsbDeviceInfo device) {
        try {
            if(device.MountPoints.Count > 0) {
                foreach(var mountPoint in device.MountPoints) {
                    var efiPath = Path.Combine(mountPoint, "EFI", "BOOT");
                    if(Directory.Exists(efiPath)) {
                        return true;
                    }
                }
            }
        }
        catch {
            return false;
        }
        return false;
    }
    
    private async Task<BootloaderType> DetectBootloaderAsync(UsbDeviceInfo device, CancellationToken ct) {
        await Task.CompletedTask;
        
        try {
            if(device.MountPoints.Count == 0) {
                return BootloaderType.Unknown;
            }
            
            foreach(var mountPoint in device.MountPoints) {
                if(CheckWindowsBootloader(mountPoint)) return BootloaderType.WindowsBoot;
                if(CheckGRUBBootloader(mountPoint)) return BootloaderType.GRUB;
                if(CheckSyslinuxBootloader(mountPoint)) return BootloaderType.Syslinux;
                if(CheckUEFIBootloader(mountPoint)) return BootloaderType.UEFIGeneric;
            }
        }
        catch {
            return BootloaderType.Unknown;
        }
        
        return BootloaderType.None;
    }
    
    private bool CheckWindowsBootloader(string volume) {
        try {
            if(File.Exists(Path.Combine(volume, "bootmgr"))) return true;
            if(Directory.Exists(Path.Combine(volume, "Boot"))) return true;
            if(Directory.Exists(Path.Combine(volume, "EFI", "Microsoft", "Boot"))) return true;
        }
        catch { }
        return false;
    }
    
    private bool CheckGRUBBootloader(string volume) {
        try {
            if(Directory.Exists(Path.Combine(volume, "boot", "grub"))) return true;
            if(Directory.Exists(Path.Combine(volume, "grub"))) return true;
            if(File.Exists(Path.Combine(volume, "EFI", "BOOT", "grubx64.efi"))) return true;
        }
        catch { }
        return false;
    }
    
    private bool CheckSyslinuxBootloader(string volume) {
        try {
            if(File.Exists(Path.Combine(volume, "syslinux.cfg"))) return true;
            if(File.Exists(Path.Combine(volume, "isolinux.cfg"))) return true;
            if(File.Exists(Path.Combine(volume, "ldlinux.sys"))) return true;
        }
        catch { }
        return false;
    }
    
    private bool CheckUEFIBootloader(string volume) {
        try {
            var efiBootPath = Path.Combine(volume, "EFI", "BOOT");
            if(Directory.Exists(efiBootPath)) {
                var efiFiles = Directory.GetFiles(efiBootPath, "*.efi");
                return efiFiles.Length > 0;
            }
        }
        catch { }
        return false;
    }
    
    private void DetermineBootability(BootVerificationResult result) {
        if(result.IsGPT && result.HasESP && result.DetectedBootloader != BootloaderType.None) {
            result.IsBootable = true;
            result.BootMode = result.HasValidMBRSignature && result.HasActivePartition 
                ? BootMode.Hybrid 
                : BootMode.UEFI;
            return;
        }
        
        if(result.HasValidMBRSignature && result.HasActivePartition && result.DetectedBootloader != BootloaderType.None) {
            result.IsBootable = true;
            result.BootMode = BootMode.Legacy;
            return;
        }
        
        result.IsBootable = false;
        result.BootMode = BootMode.Unknown;
        
        if(!result.HasValidMBRSignature) {
            result.Warnings.Add("Invalid MBR signature");
        }
        if(!result.HasActivePartition && !result.IsGPT) {
            result.Warnings.Add("No active partition");
        }
        if(result.DetectedBootloader == BootloaderType.None) {
            result.Warnings.Add("No bootloader detected");
        }
    }
    
    private async Task<byte[]> ReadBootSectorAsync(string physicalPath, CancellationToken ct) {
        return await ReadSectorAsync(physicalPath, 0, ct);
    }
    
    private async Task<byte[]> ReadSectorAsync(string physicalPath, long sectorNumber, CancellationToken ct) {
        const int SECTOR_SIZE = 512;
        var buffer = new byte[SECTOR_SIZE];
        
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
            throw new IOException($"Cannot open device {physicalPath}");
        }
        
        try {
            using(var stream = new FileStream(handle, FileAccess.Read)) {
                stream.Seek(sectorNumber * SECTOR_SIZE, SeekOrigin.Begin);
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, SECTOR_SIZE), ct);
                if(bytesRead < SECTOR_SIZE) {
                    Array.Resize(ref buffer, bytesRead);
                }
            }
        }
        finally {
            handle.Close();
        }
        
        return buffer;
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


