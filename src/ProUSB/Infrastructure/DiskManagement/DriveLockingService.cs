using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using ProUSB.Infrastructure.DiskManagement.Native;
using ProUSB.Services.Logging;

namespace ProUSB.Infrastructure.DiskManagement;

public class DriveLockingService {
    private readonly FileLogger _log;
    private const int DRIVE_ACCESS_RETRIES = 20;
    private const int DRIVE_ACCESS_TIMEOUT = 5000; 

    public DriveLockingService(FileLogger log) {
        _log = log;
    }

    public SafeFileHandle OpenAndLockDrive(int diskIndex, bool writeAccess) {
        string path = $@"\\.\PhysicalDrive{diskIndex}";
        SafeFileHandle hDrive = new SafeFileHandle(IntPtr.Zero, true);
        bool bWriteShare = false;

        for (int i = 0; i < DRIVE_ACCESS_RETRIES; i++) {

            
            uint access = NativeMethods.GENERIC_READ | (writeAccess ? NativeMethods.GENERIC_WRITE : 0);
            uint share = NativeMethods.FILE_SHARE_READ | (bWriteShare ? NativeMethods.FILE_SHARE_WRITE : 0);

            hDrive = NativeMethods.CreateFile(
                path,
                access,
                share,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_ATTRIBUTE_NORMAL | NativeMethods.FILE_FLAG_NO_BUFFERING | NativeMethods.FILE_FLAG_WRITE_THROUGH,
                IntPtr.Zero
            );

            if (!hDrive.IsInvalid) {
                break; 
            }

            int error = Marshal.GetLastWin32Error();
            if (error != 32 && error != 5) { 

                throw new Win32Exception(error, $"Failed to open drive {path}");
            }

            if (i == 0) {
                _log.Info($"Waiting for access on {path}...");
            } else if (!bWriteShare && i > DRIVE_ACCESS_RETRIES / 3) {
                _log.Warn("Could not obtain exclusive rights. Retrying with write sharing enabled...");
                bWriteShare = true;
            }

            Thread.Sleep(DRIVE_ACCESS_TIMEOUT / DRIVE_ACCESS_RETRIES);
        }

        if (hDrive.IsInvalid) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not open {path} after {DRIVE_ACCESS_RETRIES} retries.");
        }

        if (writeAccess) {
            if (!LockDrive(hDrive)) {
                hDrive.Dispose();
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not lock drive {path}");
            }
        }

        return hDrive;
    }

    private bool LockDrive(SafeFileHandle hDrive) {
        uint bytesReturned;

        if (NativeMethods.DeviceIoControl(hDrive, NativeMethods.FSCTL_ALLOW_EXTENDED_DASD_IO, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero)) {
            _log.Info("I/O boundary checks disabled (FSCTL_ALLOW_EXTENDED_DASD_IO success)");
        }

        long endTime = DateTime.Now.Ticks + (DRIVE_ACCESS_TIMEOUT * 10000);
        
        while (DateTime.Now.Ticks < endTime) {
            if (NativeMethods.DeviceIoControl(hDrive, NativeMethods.FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero)) {
                _log.Info("FSCTL_LOCK_VOLUME success");
                return true;
            }
            Thread.Sleep(DRIVE_ACCESS_TIMEOUT / DRIVE_ACCESS_RETRIES);
        }

        _log.Warn($"Could not lock drive: {Marshal.GetLastWin32Error()}");
        return false;
    }
}



