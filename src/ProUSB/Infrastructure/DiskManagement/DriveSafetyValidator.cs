using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ProUSB.Services.Logging;
using ProUSB.Domain;

namespace ProUSB.Infrastructure.DiskManagement;

public class DriveSafetyValidator {
    private readonly FileLogger _log;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetSystemDirectory(
        [Out] System.Text.StringBuilder lpBuffer,
        uint uSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetVolumePathName(
        string lpszFileName,
        [Out] System.Text.StringBuilder lpszVolumePathName,
        uint cchBufferLength);

    public enum SafetyViolation {
        None = 0,
        IsSystemDrive = 1,
        IsSourceDrive = 2,
        TooSmall = 4,
        IsReadOnly = 8
    }

    public DriveSafetyValidator(FileLogger log) {
        _log = log;
    }

    public SafetyViolation ValidateDrive(UsbDeviceInfo deviceInfo, string? isoPath = null) {
        SafetyViolation violations = SafetyViolation.None;

        if (IsSystemDrive(deviceInfo)) {
            violations |= SafetyViolation.IsSystemDrive;
            _log.Warn($"⚠️ CRITICAL: Device {deviceInfo.FriendlyName} is a SYSTEM DRIVE!");
        }

        if (!string.IsNullOrEmpty(isoPath) && IsSourceDrive(deviceInfo, isoPath)) {
            violations |= SafetyViolation.IsSourceDrive;
            _log.Warn($"⚠️ WARNING: Device {deviceInfo.FriendlyName} contains the source ISO!");
        }

        if (deviceInfo.TotalSize < 64 * 1024 * 1024) {
            violations |= SafetyViolation.TooSmall;
            _log.Warn($"⚠️ WARNING: Device {deviceInfo.FriendlyName} is too small ({deviceInfo.TotalSize} bytes)");
        }

        return violations;
    }

    private bool IsSystemDrive(UsbDeviceInfo deviceInfo) {
        try {

            var sysDir = new System.Text.StringBuilder(260);
            GetSystemDirectory(sysDir, 260);
            string windowsPath = sysDir.ToString();

            if (string.IsNullOrEmpty(windowsPath)) {
                _log.Warn("Could not determine Windows directory");
                return false;
            }

            string systemRoot = Path.GetPathRoot(windowsPath) ?? "";

            foreach (var mountPoint in deviceInfo.MountPoints) {
                string driveLetter = mountPoint.TrimEnd('\\');
                if (driveLetter.Equals(systemRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        } catch (Exception ex) {
            _log.Warn($"Error checking system drive: {ex.Message}");

            return true;
        }
    }

    private bool IsSourceDrive(UsbDeviceInfo deviceInfo, string isoPath) {
        try {
            if (!File.Exists(isoPath)) {
                return false;
            }

            var volumePath = new System.Text.StringBuilder(260);
            if (!GetVolumePathName(isoPath, volumePath, 260)) {
                _log.Warn($"Could not get volume path for ISO: {isoPath}");
                return false;
            }

            string isoRoot = volumePath.ToString().TrimEnd('\\');

            foreach (var mountPoint in deviceInfo.MountPoints) {
                string driveLetter = mountPoint.TrimEnd('\\');
                if (driveLetter.Equals(isoRoot, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        } catch (Exception ex) {
            _log.Warn($"Error checking source drive: {ex.Message}");
            return false;
        }
    }

    public string GetViolationMessage(SafetyViolation violations) {
        if (violations == SafetyViolation.None) {
            return "Drive is safe to format.";
        }

        var messages = new System.Collections.Generic.List<string>();

        if ((violations & SafetyViolation.IsSystemDrive) != 0) {
            messages.Add("🛑 CRITICAL: This is your SYSTEM DRIVE (contains Windows). Formatting it will destroy your operating system!");
        }

        if ((violations & SafetyViolation.IsSourceDrive) != 0) {
            messages.Add("⚠️ WARNING: This drive contains the ISO file you're trying to burn. Choose a different target.");
        }

        if ((violations & SafetyViolation.TooSmall) != 0) {
            messages.Add("⚠️ WARNING: This drive is unusually small. It may be corrupted.");
        }

        if ((violations & SafetyViolation.IsReadOnly) != 0) {
            messages.Add("⚠️ WARNING: This drive is read-only.");
        }

        return string.Join("\n", messages);
    }

    public bool IsCritical(SafetyViolation violations) {
        return (violations & (SafetyViolation.IsSystemDrive | SafetyViolation.IsSourceDrive)) != 0;
    }
}


