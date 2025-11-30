using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ProUSB.Infrastructure.DiskManagement.Native;

internal static class NativeMethods {
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
    public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    public const uint IOCTL_DISK_GET_DRIVE_LAYOUT_EX = 0x00074050;
    public const uint IOCTL_DISK_SET_DRIVE_LAYOUT_EX = 0x0007C054;
    public const uint IOCTL_DISK_CREATE_DISK = 0x0007C058;
    public const uint IOCTL_DISK_DELETE_DRIVE_LAYOUT = 0x0007C0E0;
    public const uint IOCTL_DISK_UPDATE_PROPERTIES = 0x00070024;
    public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY_EX = 0x000700A0;
    public const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;
    public const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
    public const uint FSCTL_LOCK_VOLUME = 0x00090018;
    public const uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
    public const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
    public const uint FSCTL_ALLOW_EXTENDED_DASD_IO = 0x00090083;

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

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteFile(
        SafeFileHandle hFile,
        IntPtr lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadFile(
        SafeFileHandle hFile,
        IntPtr lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetFilePointerEx(
        SafeFileHandle hFile,
        long liDistanceToMove,
        IntPtr lpNewFilePointer,
        uint dwMoveMethod);

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_DISK {
        public int PartitionStyle;
        public CREATE_DISK_UNION Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CREATE_DISK_UNION {
        [FieldOffset(0)] public CREATE_DISK_MBR Mbr;
        [FieldOffset(0)] public CREATE_DISK_GPT Gpt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_DISK_MBR {
        public uint Signature;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_DISK_GPT {
        public Guid DiskId;
        public uint MaxPartitionCount;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DRIVE_LAYOUT_INFORMATION_EX {
        public uint PartitionStyle;
        public uint PartitionCount;
        public DRIVE_LAYOUT_INFORMATION_UNION Union;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public PARTITION_INFORMATION_EX[] PartitionEntry;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct DRIVE_LAYOUT_INFORMATION_UNION {
        [FieldOffset(0)] public DRIVE_LAYOUT_INFORMATION_MBR Mbr;
        [FieldOffset(0)] public DRIVE_LAYOUT_INFORMATION_GPT Gpt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DRIVE_LAYOUT_INFORMATION_MBR {
        public uint Signature;
        public uint CheckSum;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DRIVE_LAYOUT_INFORMATION_GPT {
        public Guid DiskId;
        public long StartingUsableOffset;
        public long UsableLength;
        public uint MaxPartitionCount;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct PARTITION_INFORMATION_EX {
        public uint PartitionStyle;
        public long StartingOffset;
        public long PartitionLength;
        public uint PartitionNumber;
        [MarshalAs(UnmanagedType.U1)]
        public bool RewritePartition;
        [MarshalAs(UnmanagedType.U1)]
        public bool IsServicePartition;
        public PARTITION_INFORMATION_UNION Union;
    }

    [StructLayout(LayoutKind.Explicit, Size = 112)]
    public unsafe struct PARTITION_INFORMATION_UNION {
        [FieldOffset(0)] public PARTITION_INFORMATION_MBR Mbr;
        [FieldOffset(0)] public PARTITION_INFORMATION_GPT Gpt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PARTITION_INFORMATION_MBR {
        public byte PartitionType;
        [MarshalAs(UnmanagedType.U1)]
        public bool BootIndicator;
        [MarshalAs(UnmanagedType.U1)]
        public bool RecognizedPartition;
        public byte Padding1;
        public uint HiddenSectors;
        public Guid PartitionId;
        public fixed byte Padding2[88];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PARTITION_INFORMATION_GPT {
        public Guid PartitionType;
        public Guid PartitionId;
        public ulong Attributes;
        public fixed byte NameBytes[72];
        
        public void SetName(string value) {
            fixed (byte* pName = NameBytes) {
                for (int i = 0; i < 72; i++) {
                    pName[i] = 0;
                }
                
                if (!string.IsNullOrEmpty(value)) {
                    byte[] bytes = System.Text.Encoding.Unicode.GetBytes(value);
                    int copyLen = Math.Min(bytes.Length, 72);
                    for (int i = 0; i < copyLen; i++) {
                        pName[i] = bytes[i];
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISK_GEOMETRY {
        public long Cylinders;
        public uint MediaType;
        public uint TracksPerCylinder;
        public uint SectorsPerTrack;
        public uint BytesPerSector;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISK_GEOMETRY_EX {
        public DISK_GEOMETRY Geometry;
        public long DiskSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISK_EXTENT {
        public int DiskNumber;
        public long StartingOffset;
        public long ExtentLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VOLUME_DISK_EXTENTS {
        public int NumberOfDiskExtents;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public DISK_EXTENT[] Extents;
    }

    public enum PARTITION_STYLE : int {
        MBR = 0,
        GPT = 1,
        RAW = 2
    }

    public const ulong GPT_ATTRIBUTE_PLATFORM_REQUIRED = 0x0000000000000001;
    public const ulong GPT_ATTRIBUTE_NO_BLOCK_IO_PROTOCOL = 0x0000000000000002;
    public const ulong GPT_ATTRIBUTE_LEGACY_BIOS_BOOTABLE = 0x0000000000000004;
    public const ulong GPT_BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER = 0x8000000000000000;
    public const ulong GPT_BASIC_DATA_ATTRIBUTE_HIDDEN = 0x4000000000000000;
    public const ulong GPT_BASIC_DATA_ATTRIBUTE_SHADOW_COPY = 0x2000000000000000;
    public const ulong GPT_BASIC_DATA_ATTRIBUTE_READ_ONLY = 0x1000000000000000;
    
    [DllImport("fmifs.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern void FormatEx(
        string DriveRoot,
        int MediaFlag,
        string FileSystem,
        string Label,
        bool QuickFormat,
        int ClusterSize,
        FormatCallback Callback);

    public delegate bool FormatCallback(
        int Command,
        int SubAction,
        IntPtr ActionInfo);

    public enum FMIFS_PACKET_TYPE {
        FmIfsPercentCompleted = 0,
        FmIfsFormatReport = 1,
        FmIfsInsertDisk = 2,
        FmIfsIncompatibleFileSystem = 3,
        FmIfsFormattingDestination = 4,
        FmIfsIncompatibleMedia = 5,
        FmIfsAccessDenied = 6,
        FmIfsMediaWriteProtected = 7,
        FmIfsCantLock = 8,
        FmIfsCantQuickFormat = 9,
        FmIfsIoError = 10,
        FmIfsFinished = 11,
        FmIfsBadLabel = 12
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_PROPERTY_QUERY {
        public uint PropertyId;
        public uint QueryType;
        public byte AdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_DEVICE_DESCRIPTOR_HEADER {
        public uint Version;
        public uint Size;
        public byte DeviceType;
        public byte DeviceTypeModifier;
        [MarshalAs(UnmanagedType.U1)] public bool RemovableMedia;
        [MarshalAs(UnmanagedType.U1)] public bool CommandQueueing;
        public uint VendorIdOffset;
        public uint ProductIdOffset;
        public uint ProductRevisionOffset;
        public uint SerialNumberOffset;
        public uint BusType;
        public uint RawPropertiesLength;
    }
}

