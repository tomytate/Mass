using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using ProUSB.Services.Logging;

namespace ProUSB.Infrastructure.DiskManagement;

public class AdvancedVolumeMounter {
    private readonly FileLogger _log;

    public AdvancedVolumeMounter(FileLogger log) {
        _log = log;
    }

    public async Task<string> WaitForVolumeAsync(int diskIndex, long partitionOffset, int timeoutSeconds, CancellationToken ct, bool wildcard = false) {
        _log.Info($"=== ADVANCED VOLUME MOUNT (Multi-API Technique) ===");
        _log.Info($"Waiting for volume on Disk {diskIndex}, Partition offset {partitionOffset}, Wildcard: {wildcard}");

        var startTime = DateTime.UtcNow;
        string? volumePath = null;

        await TriggerMultipleRefreshMethods(diskIndex, ct);

        int attempts = 0;
        int maxAttempts = timeoutSeconds * 2;

        while (attempts < maxAttempts && volumePath == null) {
            ct.ThrowIfCancellationRequested();
            
            volumePath = GetVolumeForPartition(diskIndex, partitionOffset, wildcard);
            
            if (volumePath != null) {
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                _log.Info($"Volume found: {volumePath} (after {elapsed:F1}s)");
                
                if (await WaitForVolumeReady(volumePath, ct)) {
                    return volumePath;
                }
            }

            if (attempts % 10 == 0 && attempts > 0) {
                _log.Debug($"Still waiting... {attempts * 0.5:F1}s / {timeoutSeconds}s");
                
                if (attempts % 20 == 0) {
                    _log.Info("Triggering additional refresh...");
                    await TriggerMultipleRefreshMethods(diskIndex, ct);
                }
            }

            await Task.Delay(500, ct);
            attempts++;
        }

        var totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        throw new MountFailedException(
            $"Volume did not appear after {totalElapsed:F1}s. " +
            $"Disk {diskIndex} may need manual drive letter assignment in Disk Management.");
    }

    public async Task TriggerMultipleRefreshMethods(int diskIndex, CancellationToken ct) {
        _log.Info("Triggering comprehensive partition refresh...");

        await Task.Run(() => {
            RefreshDiskLayout(diskIndex);
        }, ct);

        var vdsRefresher = new Vds.VdsPartitionRefresher(_log);
        await vdsRefresher.RefreshPartitionsAsync(ct);

        TriggerDeviceArrival();

        await Task.Delay(500, ct);
    }

    private void RefreshDiskLayout(int diskIndex) {
        try {
            using var hDrive = NativeMethods.CreateFile(
                $@"\\.\PhysicalDrive{diskIndex}",
                NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (hDrive.IsInvalid) return;

            NativeMethods.DeviceIoControl(
                hDrive,
                NativeMethods.IOCTL_DISK_UPDATE_PROPERTIES,
                IntPtr.Zero, 0, IntPtr.Zero, 0,
                out _, IntPtr.Zero);

            int layoutSize = 4096;
            IntPtr layoutPtr = Marshal.AllocHGlobal(layoutSize);
            try {
                NativeMethods.DeviceIoControl(
                    hDrive,
                    NativeMethods.IOCTL_DISK_GET_DRIVE_LAYOUT_EX,
                    IntPtr.Zero, 0, layoutPtr, (uint)layoutSize,
                    out _, IntPtr.Zero);
            } finally {
                Marshal.FreeHGlobal(layoutPtr);
            }

        } catch (Exception ex) {
            _log.Debug($"RefreshDiskLayout error: {ex.Message}");
        }
    }

    private void TriggerDeviceArrival() {
        try {
            IntPtr hWnd = NativeMethods.HWND_BROADCAST;
            IntPtr result = IntPtr.Zero;
            
            NativeMethods.SendMessageTimeout(
                hWnd,
                NativeMethods.WM_DEVICECHANGE,
                new IntPtr(0x8000),
                IntPtr.Zero,
                0,
                1000,
                out result);

            _log.Debug("Broadcasted device arrival message");
        } catch (Exception ex) {
            _log.Debug($"TriggerDeviceArrival error: {ex.Message}");
        }
    }

    public string[] GetVolumesOnDisk(int diskIndex) {
        var result = new System.Collections.Generic.List<string>();
        try {
            var volumes = EnumerateVolumes();
            foreach (var volume in volumes) {
                var extents = GetVolumeExtents(volume);
                if (extents.Any(e => e.DiskNumber == diskIndex)) {
                     result.Add(volume.TrimEnd('\\')); 
                }
            }
        } catch (Exception ex) {
            _log.Debug($"GetVolumesOnDisk error: {ex.Message}");
        }
        return result.ToArray();
    }

    public string? GetVolumeForPartition(int diskIndex, long partitionOffset, bool wildcard) {
        try {
            var volumes = EnumerateVolumes();
            
            foreach (var volume in volumes) {
                var extents = GetVolumeExtents(volume);
                
                if (extents.Any(e => e.DiskNumber == diskIndex && (wildcard || e.StartingOffset == partitionOffset))) {
                    var mountPoint = GetVolumeMountPoint(volume);
                    if (mountPoint != null) {
                        return mountPoint;
                    }
                }
            }
        } catch (Exception ex) {
            _log.Debug($"GetVolumeForPartition error: {ex.Message}");
        }

        return null;
    }

    private string[] EnumerateVolumes() {
        var volumes = new System.Collections.Generic.List<string>();
        var volumeName = new System.Text.StringBuilder(260);
        
        IntPtr hFind = NativeMethods.FindFirstVolume(volumeName, volumeName.Capacity);
        
        if (hFind != IntPtr.Zero && hFind.ToInt64() != -1) {
            try {
                do {
                    volumes.Add(volumeName.ToString());
                } while (NativeMethods.FindNextVolume(hFind, volumeName, volumeName.Capacity));
            } finally {
                NativeMethods.FindVolumeClose(hFind);
            }
        }

        return volumes.ToArray();
    }

    private (int DiskNumber, long StartingOffset)[] GetVolumeExtents(string volumeName) {
        var extents = new System.Collections.Generic.List<(int, long)>();

        try {
            var trimmed = volumeName.TrimEnd('\\');
            using var hVolume = NativeMethods.CreateFile(
                trimmed,
                0,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (hVolume.IsInvalid) return extents.ToArray();

            int size = Marshal.SizeOf<VOLUME_DISK_EXTENTS>() + 32 * Marshal.SizeOf<DISK_EXTENT>();
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try {
                if (NativeMethods.DeviceIoControl(
                    hVolume,
                    IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS,
                    IntPtr.Zero, 0,
                    buffer, (uint)size,
                    out uint bytesReturned,
                    IntPtr.Zero)) {

                    var vde = Marshal.PtrToStructure<VOLUME_DISK_EXTENTS>(buffer);
                    IntPtr extentPtr = IntPtr.Add(buffer, 8);

                    for (int i = 0; i < vde.NumberOfDiskExtents; i++) {
                        var extent = Marshal.PtrToStructure<DISK_EXTENT>(extentPtr);
                        extents.Add((extent.DiskNumber, extent.StartingOffset));
                        extentPtr = IntPtr.Add(extentPtr, Marshal.SizeOf<DISK_EXTENT>());
                    }
                }
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        } catch { }

        return extents.ToArray();
    }

    private string? GetVolumeMountPoint(string volumeName) {
        var pathNames = new System.Text.StringBuilder(1024);
        uint returnLength = 0;

        if (NativeMethods.GetVolumePathNamesForVolumeName(
            volumeName,
            pathNames,
            (uint)pathNames.Capacity,
            ref returnLength)) {

            var paths = pathNames.ToString().Split('\0', StringSplitOptions.RemoveEmptyEntries);
            return paths.FirstOrDefault();
        }

        return null;
    }

    private async Task<bool> WaitForVolumeReady(string volumePath, CancellationToken ct) {
        for (int i = 0; i < 10; i++) {
            try {
                if (Directory.Exists(volumePath)) {
                    var di = new DriveInfo(volumePath);
                    if (di.IsReady) {
                        _log.Info($"Volume {volumePath} is ready");
                        return true;
                    }
                }
            } catch { }

            await Task.Delay(200, ct);
        }

        return false;
    }

    private const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;

    [StructLayout(LayoutKind.Sequential)]
    private struct VOLUME_DISK_EXTENTS {
        public uint NumberOfDiskExtents;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISK_EXTENT {
        public int DiskNumber;
        public long StartingOffset;
        public long ExtentLength;
    }

    private static class NativeMethods {
        public const uint GENERIC_READ = 0x80000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;
        public const uint IOCTL_DISK_UPDATE_PROPERTIES = 0x00070140;
        public const uint IOCTL_DISK_GET_DRIVE_LAYOUT_EX = 0x00070050;
        public const uint WM_DEVICECHANGE = 0x0219;
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindFirstVolume(
            System.Text.StringBuilder lpszVolumeName,
            int cchBufferLength);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool FindNextVolume(
            IntPtr hFindVolume,
            System.Text.StringBuilder lpszVolumeName,
            int cchBufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindVolumeClose(IntPtr hFindVolume);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetVolumePathNamesForVolumeName(
            string lpszVolumeName,
            System.Text.StringBuilder lpszVolumePathNames,
            uint cchBufferLength,
            ref uint lpcchReturnLength);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);
    }
}


