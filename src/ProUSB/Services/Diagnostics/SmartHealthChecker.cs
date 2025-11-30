using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Services.Logging;
using ProUSB.Domain;

namespace ProUSB.Services.Diagnostics;

public enum DriveHealthStatus {
    Unknown,
    Healthy,
    Warning,
    Critical,
    Failing
}

public record SmartHealthReport {
    public DriveHealthStatus Status { get; init; }
    public int HealthScore { get; init; }
    public long ReallocatedSectorsCount { get; init; }
    public long CurrentPendingSectorCount { get; init; }
    public long UncorrectableSectorCount { get; init; }
    public int Temperature { get; init; }
    public string Message { get; init; } = "";
}

public class SmartHealthChecker {
    private readonly FileLogger _log;

    public SmartHealthChecker(FileLogger log, LogLevel minLevel = LogLevel.Info) {
        _log = log;
        _log.MinLevel = minLevel;
    }

    public async Task<SmartHealthReport> CheckDriveHealthAsync(int diskIndex, CancellationToken ct) {
        return await Task.Run(() => CheckDriveHealth(diskIndex), ct);
    }

    private SmartHealthReport CheckDriveHealth(int diskIndex) {
        _log.Info($"Checking SMART health for disk {diskIndex}...");

        try {
            using var hDrive = NativeMethods.CreateFile(
                $@"\\.\PhysicalDrive{diskIndex}",
                NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (hDrive.IsInvalid) {
                _log.Warn("Cannot open drive for SMART check");
                return new SmartHealthReport {
                    Status = DriveHealthStatus.Unknown,
                    Message = "Unable to access drive for health check"
                };
            }

            var smartData = ReadSmartData(hDrive);
            if (smartData == null) {
                _log.Info("SMART not available for this drive");
                return new SmartHealthReport {
                    Status = DriveHealthStatus.Unknown,
                    Message = "SMART not supported on this device (likely USB flash drive)"
                };
            }

            var report = AnalyzeSmartData(smartData);
            _log.Info($"SMART health: {report.Status}, Score: {report.HealthScore}");
            return report;

        } catch (Exception ex) {
            _log.Error($"SMART check failed: {ex.Message}", ex);
            throw new SmartHealthException($"SMART check failed: {ex.Message}", ex);
        }
    }

    private byte[]? ReadSmartData(SafeFileHandle hDrive) {
        const uint IOCTL_ATA_PASS_THROUGH = 0x0004D02C;
        const uint ATA_FLAGS_DATA_IN = 0x02;

        var ataPassThrough = new ATA_PASS_THROUGH_EX {
            Length = (ushort)Marshal.SizeOf<ATA_PASS_THROUGH_EX>(),
            AtaFlags = (ushort)ATA_FLAGS_DATA_IN,
            DataTransferLength = 512,
            TimeOutValue = 3,
            DataBufferOffset = (IntPtr)Marshal.SizeOf<ATA_PASS_THROUGH_EX>(),
            CurrentTaskFile = new byte[8]
        };

        ataPassThrough.CurrentTaskFile[6] = 0xB0;
        ataPassThrough.CurrentTaskFile[1] = 0xD0;
        ataPassThrough.CurrentTaskFile[2] = 0x01;
        ataPassThrough.CurrentTaskFile[3] = 0x00;
        ataPassThrough.CurrentTaskFile[4] = 0x4F;
        ataPassThrough.CurrentTaskFile[5] = 0xC2;

        int bufferSize = Marshal.SizeOf<ATA_PASS_THROUGH_EX>() + 512;
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

        try {
            Marshal.StructureToPtr(ataPassThrough, buffer, false);

            bool success = NativeMethods.DeviceIoControl(
                hDrive,
                IOCTL_ATA_PASS_THROUGH,
                buffer,
                (uint)bufferSize,
                buffer,
                (uint)bufferSize,
                out uint bytesReturned,
                IntPtr.Zero);

            if (!success) {
                return null;
            }

            byte[] smartData = new byte[512];
            IntPtr dataPtr = IntPtr.Add(buffer, Marshal.SizeOf<ATA_PASS_THROUGH_EX>());
            Marshal.Copy(dataPtr, smartData, 0, 512);

            return smartData;

        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private SmartHealthReport AnalyzeSmartData(byte[] data) {
        long reallocated = 0;
        long pending = 0;
        long uncorrectable = 0;
        int temperature = 0;

        for (int i = 2; i < 362; i += 12) {
            byte id = data[i];
            ushort raw = BitConverter.ToUInt16(data, i + 5);

            switch (id) {
                case 5:
                    reallocated = raw;
                    break;
                case 196:
                    pending = raw;
                    break;
                case 198:
                case 197:
                    uncorrectable = raw;
                    break;
                case 194:
                    temperature = raw;
                    break;
            }
        }

        int healthScore = 100;
        var status = DriveHealthStatus.Healthy;
        string message = "Drive health is good";

        if (reallocated > 0) {
            healthScore -= (int)(reallocated * 5);
            status = DriveHealthStatus.Warning;
            message = $"Warning: {reallocated} reallocated sectors detected";
        }

        if (pending > 0) {
            healthScore -= (int)(pending * 10);
            status = DriveHealthStatus.Critical;
            message = $"Critical: {pending} pending sectors detected";
        }

        if (uncorrectable > 0) {
            healthScore -= (int)(uncorrectable * 15);
            status = DriveHealthStatus.Failing;
            message = $"Drive failing: {uncorrectable} uncorrectable errors";
        }

        if (temperature > 60) {
            healthScore -= (temperature - 60) * 2;
            if (status == DriveHealthStatus.Healthy) {
                status = DriveHealthStatus.Warning;
                message = $"Drive temperature high: {temperature}°C";
            }
        }

        healthScore = Math.Max(0, healthScore);

        return new SmartHealthReport {
            Status = status,
            HealthScore = healthScore,
            ReallocatedSectorsCount = reallocated,
            CurrentPendingSectorCount = pending,
            UncorrectableSectorCount = uncorrectable,
            Temperature = temperature,
            Message = message
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ATA_PASS_THROUGH_EX {
        public ushort Length;
        public ushort AtaFlags;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte ReservedAsUchar;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public uint ReservedAsUlong;
        public IntPtr DataBufferOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] PreviousTaskFile;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] CurrentTaskFile;
    }

    private static class NativeMethods {
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;

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
    }
}


