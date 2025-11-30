using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ProUSB.Services.Logging;

namespace ProUSB.Infrastructure.DiskManagement;

public class AutoMountManager {
    private readonly FileLogger _log;
    private const string MOUNTMGR_DOS_DEVICE_NAME = @"\\.\MountPointManager";
    private const uint IOCTL_MOUNTMGR_SET_AUTO_MOUNT = 0x006DC034;
    private const uint IOCTL_MOUNTMGR_QUERY_AUTO_MOUNT = 0x006DC038;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,

        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref bool lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        IntPtr lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        ref bool lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        IntPtr lpOverlapped);

    public AutoMountManager(FileLogger log) {
        _log = log;
    }

    public bool SetAutoMount(bool enable) {
        using var hMountMgr = CreateFile(
            MOUNTMGR_DOS_DEVICE_NAME,
            0, 
            3, 
            IntPtr.Zero,
            3, 
            0x80, 
            IntPtr.Zero
        );

        if (hMountMgr.IsInvalid) {
            _log.Warn($"Failed to open MountPointManager: {Marshal.GetLastWin32Error()}");
            return false;
        }

        bool enableValue = enable;
        bool result = DeviceIoControl(
            hMountMgr,
            IOCTL_MOUNTMGR_SET_AUTO_MOUNT,
            ref enableValue,
            sizeof(bool),
            IntPtr.Zero,
            0,
            IntPtr.Zero,
            IntPtr.Zero
        );

        if (result) {
            _log.Info($"AutoMount {(enable ? "enabled" : "disabled")} successfully");
        } else {
            _log.Warn($"Failed to set AutoMount: {Marshal.GetLastWin32Error()}");
        }

        return result;
    }

    public bool GetAutoMount(out bool enabled) {
        enabled = false;
        
        using var hMountMgr = CreateFile(
            MOUNTMGR_DOS_DEVICE_NAME,
            0,
            3,
            IntPtr.Zero,
            3,
            0x80,
            IntPtr.Zero
        );

        if (hMountMgr.IsInvalid) {
            return false;
        }

        bool result = DeviceIoControl(
            hMountMgr,
            IOCTL_MOUNTMGR_QUERY_AUTO_MOUNT,
            IntPtr.Zero,
            0,
            ref enabled,
            sizeof(bool),
            out _,
            IntPtr.Zero
        );

        return result;
    }

    public void WithAutoMountDisabled(Action action) {
        bool wasEnabled = false;
        bool savedState = GetAutoMount(out wasEnabled);

        try {
            if (wasEnabled) {
                _log.Info("Temporarily disabling AutoMount for disk operation...");
                SetAutoMount(false);
            }

            action();
        } finally {
            if (savedState && wasEnabled) {
                _log.Info("Restoring AutoMount to enabled state...");
                SetAutoMount(true);
            }
        }
    }
}



