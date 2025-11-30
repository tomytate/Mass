using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ProUSB.Infrastructure.DiskManagement.Vds;

[ComImport]
[Guid("B6B22DA8-F903-4BE7-B492-C09D875AC9DA")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IVdsServiceLoader {
    [PreserveSig]
    int LoadService([In, MarshalAs(UnmanagedType.LPWStr)] string machineName, out IVdsService service);
}

[ComImport]
[Guid("0818A8EF-9BA9-40D8-A6F9-E22833CC771E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IVdsService {
    [PreserveSig]
    int IsServiceReady();

    [PreserveSig]
    int WaitForServiceReady();

    [PreserveSig]
    int GetProperties(out VDS_SERVICE_PROP properties);

    [PreserveSig]
    int QueryProviders(uint masks, out IEnumVdsObject enumObject);

    [PreserveSig]
    int QueryMaskedDisks(out IEnumVdsObject enumObject);

    [PreserveSig]
    int QueryUnallocatedDisks(out IEnumVdsObject enumObject);

    [PreserveSig]
    int GetObject([In, MarshalAs(UnmanagedType.Struct)] Guid objectId, VDS_OBJECT_TYPE type, out object obj);

    [PreserveSig]
    int QueryDriveLetters(byte wcFirstLetter, uint count, out VDS_DRIVE_LETTER_PROP driveLetterPropArray);

    [PreserveSig]
    int QueryFileSystemTypes(out IntPtr ppFileSystemTypeProps, out int plNumberOfFileSystems);

    [PreserveSig]
    int Reenumerate();

    [PreserveSig]
    int Refresh();

    [PreserveSig]
    int CleanupObsoleteMountPoints();
}

[ComImport]
[Guid("118610B7-8D94-4030-B5B8-500889788E4E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumVdsObject {
    [PreserveSig]
    int Next(uint celt, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown)] object[] ppObjectArray, out uint pcFetched);

    [PreserveSig]
    int Skip(uint celt);

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int Clone([MarshalAs(UnmanagedType.Interface)] out IEnumVdsObject ppEnum);
}

[StructLayout(LayoutKind.Sequential)]
public struct VDS_SERVICE_PROP {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pwszVersion;
    public uint ulFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct VDS_DRIVE_LETTER_PROP {
    public byte wcLetter;
    public Guid volumeId;
    public uint ulFlags;
    [MarshalAs(UnmanagedType.Bool)]
    public bool bUsed;
}

public enum VDS_OBJECT_TYPE {
    VDS_OT_UNKNOWN = 0,
    VDS_OT_PROVIDER = 1,
    VDS_OT_PACK = 10,
    VDS_OT_VOLUME = 11,
    VDS_OT_VOLUME_PLEX = 12,
    VDS_OT_DISK = 13
}

[ComImport]
[Guid("9AA58360-CE33-4F92-B658-ED24B14425B8")]
public class VdsServiceLoader { }

