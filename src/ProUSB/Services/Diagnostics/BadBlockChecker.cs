using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using ProUSB.Domain;
using ProUSB.Infrastructure.DiskManagement.Native;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace ProUSB.Services.Diagnostics;

public class BadBlockChecker {
    private const int BlockSize = 512 * 1024;
    private const byte Pattern = 0x55;

    public async Task CheckDiskAsync(int diskIndex, IProgress<double> progress, CancellationToken ct) {
        string drivePath = $@"\\.\PhysicalDrive{diskIndex}";
        
        using var hDrive = NativeMethods.CreateFile(
            drivePath,
            NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
            NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
            IntPtr.Zero,
            NativeMethods.OPEN_EXISTING,
            NativeMethods.FILE_FLAG_NO_BUFFERING | NativeMethods.FILE_FLAG_WRITE_THROUGH,
            IntPtr.Zero);

        if (hDrive.IsInvalid) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open disk for bad block check");
        }

        long diskSize = GetDiskSize(hDrive);
        long totalBlocks = diskSize / BlockSize;
        
        byte[] writeBuffer = new byte[BlockSize];
        for(int i=0; i<writeBuffer.Length; i++) writeBuffer[i] = Pattern;
        
        byte[] readBuffer = new byte[BlockSize];
        IntPtr writePtr = Marshal.AllocHGlobal(BlockSize);
        IntPtr readPtr = Marshal.AllocHGlobal(BlockSize);

        try {
            Marshal.Copy(writeBuffer, 0, writePtr, BlockSize);

            for (long i = 0; i < totalBlocks; i++) {
                ct.ThrowIfCancellationRequested();
                
                long offset = i * BlockSize;
                if (!NativeMethods.SetFilePointerEx(hDrive, offset, IntPtr.Zero, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!NativeMethods.WriteFile(hDrive, writePtr, (uint)BlockSize, out _, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Write failed at block {i}");

                progress?.Report((double)i / totalBlocks * 50.0);
            }

            for (long i = 0; i < totalBlocks; i++) {
                ct.ThrowIfCancellationRequested();
                
                long offset = i * BlockSize;
                if (!NativeMethods.SetFilePointerEx(hDrive, offset, IntPtr.Zero, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!NativeMethods.ReadFile(hDrive, readPtr, (uint)BlockSize, out uint read, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Read failed at block {i}");

                if (read != BlockSize) throw new Exception($"Incomplete read at block {i}");

                Marshal.Copy(readPtr, readBuffer, 0, BlockSize);
                
                for(int j=0; j<BlockSize; j++) {
                    if (readBuffer[j] != Pattern)
                        throw new Exception($"Bad block detected at offset {offset + j}");
                }

                progress?.Report(50.0 + ((double)i / totalBlocks * 50.0));
            }
        } finally {
            Marshal.FreeHGlobal(writePtr);
            Marshal.FreeHGlobal(readPtr);
        }
    }

    private long GetDiskSize(SafeFileHandle hDrive) {
        var geom = new NativeMethods.DISK_GEOMETRY_EX();
        int size = Marshal.SizeOf(geom);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try {
            if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_GET_DRIVE_GEOMETRY_EX, IntPtr.Zero, 0, ptr, (uint)size, out _, IntPtr.Zero)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            geom = Marshal.PtrToStructure<NativeMethods.DISK_GEOMETRY_EX>(ptr);
            return geom.DiskSize;
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
    }
}


