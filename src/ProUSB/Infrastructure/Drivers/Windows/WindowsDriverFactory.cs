using System;
using System.Collections.Generic;
using System.Management;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Domain.Drivers;
using ProUSB.Infrastructure.DiskManagement.Native;
namespace ProUSB.Infrastructure.Drivers.Windows;

public class WindowsDriverFactory : IDriverFactory {
    public async Task<IDiskDriver> OpenDriverAsync(string id, bool w, CancellationToken ct) {
        if(!id.StartsWith(@"\\.\PHYSICALDRIVE")) throw new ArgumentException("Bad Path");
        uint acc = w ? (NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE) : NativeMethods.GENERIC_READ;
        uint fl = NativeMethods.FILE_FLAG_NO_BUFFERING | NativeMethods.FILE_FLAG_WRITE_THROUGH | NativeMethods.FILE_FLAG_OVERLAPPED;
        var h = NativeMethods.CreateFile(id, acc, 3, IntPtr.Zero, 3, fl, IntPtr.Zero);
        if(h.IsInvalid) throw new IOException($"Open Fail: {Marshal.GetLastWin32Error()}");
        try { var g=GetG(h); var b=GetB(h); return new WindowsDiskDriver(h, id, (int)g.Geometry.BytesPerSector, g.DiskSize, b); }
        catch { h.Dispose(); throw; }
    }

    [SupportedOSPlatform("windows")]
    public async Task<IEnumerable<UsbDeviceInfo>> EnumerateDevicesAsync(CancellationToken ct) {
        return await Task.Run(() => {
            var l = new List<UsbDeviceInfo>();
            try {
                using var s=new ManagementObjectSearcher(@"SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                foreach(ManagementObject d in s.Get()) {
                    ct.ThrowIfCancellationRequested();
                    try {
                        var u = new UsbDeviceInfo{ DeviceId=d["DeviceID"]?.ToString()??"", FriendlyName=d["Caption"]?.ToString()??"", TotalSize=Convert.ToInt64(d["Size"]), BusType="USB", PhysicalIndex=PIdx(d["DeviceID"]?.ToString()) };
                        Resolv(u, u.DeviceId); l.Add(u);
                    } catch {}
                }
            } catch (Exception e) { if(e is UnauthorizedAccessException) throw; }
            return l;
        }, ct);
    }
    
    [SupportedOSPlatform("windows")]
    private void Resolv(UsbDeviceInfo d, string pid) {
        try {
            string q1=$"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{pid.Replace("\\","\\\\")}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
            using var s1=new ManagementObjectSearcher(q1);
            foreach(ManagementObject p in s1.Get()) {
                string q2=$"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{p["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
                using var s2=new ManagementObjectSearcher(q2);
                foreach(ManagementObject ld in s2.Get()) d.MountPoints.Add(ld["DeviceID"]?.ToString()!);
            }
        } catch {}
    }

    private int PIdx(string? s) => (s!=null && int.TryParse(s.Replace(@"\\.\PHYSICALDRIVE",""),out int i))?i:-1;
    private NativeMethods.DISK_GEOMETRY_EX GetG(Microsoft.Win32.SafeHandles.SafeFileHandle h) {
        int s=Marshal.SizeOf<NativeMethods.DISK_GEOMETRY_EX>(); IntPtr p=Marshal.AllocHGlobal(s);
        try{if(!NativeMethods.DeviceIoControl(h, NativeMethods.IOCTL_DISK_GET_DRIVE_GEOMETRY_EX,IntPtr.Zero,0,p,(uint)s,out _,IntPtr.Zero))throw new IOException("GeomErr");return Marshal.PtrToStructure<NativeMethods.DISK_GEOMETRY_EX>(p);}finally{Marshal.FreeHGlobal(p);}
    }
    private DeviceBusType GetB(Microsoft.Win32.SafeHandles.SafeFileHandle h) {
        int s=Marshal.SizeOf<NativeMethods.STORAGE_PROPERTY_QUERY>(); IntPtr q=Marshal.AllocHGlobal(s); IntPtr o=Marshal.AllocHGlobal(4096);
        try{
            Marshal.StructureToPtr(new NativeMethods.STORAGE_PROPERTY_QUERY(),q,false);
            if(NativeMethods.DeviceIoControl(h, NativeMethods.IOCTL_STORAGE_QUERY_PROPERTY,q,(uint)s,o,4096,out _,IntPtr.Zero)){
                var hd=Marshal.PtrToStructure<NativeMethods.STORAGE_DEVICE_DESCRIPTOR_HEADER>(o);
                if(hd.BusType==7)return DeviceBusType.USB; if(hd.BusType==17)return DeviceBusType.NVMe;
            } return DeviceBusType.Unknown;
        }finally{Marshal.FreeHGlobal(q);Marshal.FreeHGlobal(o);}
    }
}

