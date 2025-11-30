using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using ProUSB.Domain;
using ProUSB.Domain.Drivers;
using ProUSB.Infrastructure.DiskManagement.Native;

namespace ProUSB.Infrastructure.Drivers.Windows;

public sealed class WindowsDiskDriver : IDiskDriver {
    private readonly FileStream _stream;
    private readonly SafeFileHandle _handle;
    private readonly string _path;
    private readonly int _sectorSize;
    private readonly long _capacity;
    private readonly DeviceBusType _busType;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private bool _locked = false;

    internal WindowsDiskDriver(SafeFileHandle h, string p, int ss, long c, DeviceBusType b) {
        _handle=h; _path=p; _sectorSize=ss; _capacity=c; _busType=b;
        _stream = new FileStream(_handle, FileAccess.ReadWrite, _sectorSize, isAsync: true);
    }

    public string PhysicalId => _path;
    public long Capacity => _capacity;
    public int SectorSize => _sectorSize;
    public DeviceBusType BusType => _busType;

    public async Task ExclusiveLockAsync(CancellationToken ct) {
        if(_locked) return;
        await _ioLock.WaitAsync(ct);
        try {
            await Task.Run(() => {
                uint b;
                NativeMethods.DeviceIoControl(_handle, NativeMethods.FSCTL_ALLOW_EXTENDED_DASD_IO, IntPtr.Zero, 0, IntPtr.Zero, 0, out b, IntPtr.Zero);
                NativeMethods.DeviceIoControl(_handle, NativeMethods.FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out b, IntPtr.Zero);
                if(!NativeMethods.DeviceIoControl(_handle, NativeMethods.FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out b, IntPtr.Zero))
                    throw new IOException($"Lock Failed: {Marshal.GetLastWin32Error()}");
                _locked = true;
            }, ct);
        } finally { _ioLock.Release(); }
    }

    public async Task UnlockAsync(CancellationToken ct) {
        if(!_locked) return;
        await _ioLock.WaitAsync(ct);
        try {
            await Task.Run(() => {
                uint b;
                NativeMethods.DeviceIoControl(_handle, NativeMethods.FSCTL_UNLOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out b, IntPtr.Zero);
                NativeMethods.DeviceIoControl(_handle, NativeMethods.IOCTL_DISK_UPDATE_PROPERTIES, IntPtr.Zero, 0, IntPtr.Zero, 0, out b, IntPtr.Zero);
                _locked = false;
            });
        } finally { _ioLock.Release(); }
    }

    public async Task WriteSectorsAsync(long o, byte[] d, CancellationToken ct) {
        if(o%_sectorSize!=0 || d.Length%_sectorSize!=0) throw new ArgumentException("Align Error");
        await _ioLock.WaitAsync(ct);
        try { _stream.Seek(o, SeekOrigin.Begin); await _stream.WriteAsync(d, 0, d.Length, ct); }
        catch (Exception ex) { throw new IOException($"Write IO: {ex.Message}", ex); }
        finally { _ioLock.Release(); }
    }

    public async Task<byte[]> ReadSectorsAsync(long o, int cnt, CancellationToken ct) {
        int len=cnt*_sectorSize;
        if(o%_sectorSize!=0) throw new ArgumentException("Align Error");
        await _ioLock.WaitAsync(ct);
        try {
            _stream.Seek(o, SeekOrigin.Begin);
            byte[] b=new byte[len]; int r=0;
            while(r<len) { int n=await _stream.ReadAsync(b, r, len-r, ct); if(n==0) break; r+=n; }
            if(r!=len) throw new IOException("Short Read");
            return b;
        } finally { _ioLock.Release(); }
    }
    
    public void Dispose() { _stream?.Dispose(); _ioLock?.Dispose(); GC.SuppressFinalize(this); }
    public Task RefreshPartitionTableAsync(CancellationToken ct) => Task.CompletedTask;
}

