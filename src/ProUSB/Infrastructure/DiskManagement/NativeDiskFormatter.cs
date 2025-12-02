using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using ProUSB.Infrastructure.DiskManagement.Native;
using ProUSB.Services.Logging;
using ProUSB.Domain;
using System.Text;
using System.Collections.Generic;

namespace ProUSB.Infrastructure.DiskManagement;

public class NativeDiskFormatter {
    private readonly FileLogger _log;
    private readonly DriveLockingService _lockService;
    private readonly AutoMountManager _autoMountManager;
    private readonly Lock _lock = new();
    public int VolumeMountDelay { 
        get; 
        set => field = value > 0 ? value : DiskConstants.DefaultMountDelayMs; 
    } = DiskConstants.DefaultMountDelayMs;

    public class FallbackRequiredException : Exception {
        public bool UseGPT { get; }
        public long DiskSize { get; }
        
        public FallbackRequiredException(bool useGPT, long diskSize) {
            UseGPT = useGPT;
            DiskSize = diskSize;
        }
    }

    public NativeDiskFormatter(FileLogger log, LogLevel minLevel = LogLevel.Info) {
        _log = log;
        _log.MinLevel = minLevel;
        _lockService = new DriveLockingService(log);
        _autoMountManager = new AutoMountManager(log);
    }

    public async Task FormatAsync(int diskIndex, string label, string fileSystem, string partitionScheme, int clusterSize, bool quickFormat, CancellationToken ct) {
        _log.Info($"=== NATIVE FORMAT START === Disk {diskIndex}, Label: {label}, FS: {fileSystem}, Scheme: {partitionScheme}");

        _autoMountManager.WithAutoMountDisabled(() => {
            try {
                LockAndDismountVolumes(diskIndex);
                PerformDiskOperation(diskIndex, async (hDrive) => {
                     long diskSize = GetDiskSize(hDrive);
                     CleanDisk(hDrive);
                     bool useGPT = partitionScheme.ToLower().Contains("gpt");
                     InitializeDisk(hDrive, useGPT);
                     CreateSinglePartition(hDrive, useGPT, diskSize);
                     ForceRescan(hDrive);
                }, ct).Wait(ct);
            } catch (Exception ex) {
                _log.Error($"Disk preparation failed: {ex.Message}", ex);
                throw new FormatFailedException("Failed to prepare disk layout", ex);
            }
        });
        
        await Task.Delay(500, ct);
        
        bool useGPT = partitionScheme.ToLower().Contains("gpt");
        int dataPartitionNumber = DiskHelper.GetDataPartitionNumber(useGPT);
        
        long partitionOffset = useGPT ? DiskConstants.GptDataOffset : DiskConstants.MbrDataOffset;
        
        try {
            string volumePath = await WaitForVolume(diskIndex, dataPartitionNumber, partitionOffset, ct);
            
            if (fileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase)) {
                _log.Info("FAT32 detected - applying special handling for volume mounting...");
                await FormatVolumeAsync(volumePath, fileSystem, label, quickFormat, clusterSize, ct);
                await ForceVolumeOnlineAndAssignLetter(diskIndex, dataPartitionNumber, ct);
            } else {
                await FormatVolumeAsync(volumePath, fileSystem, label, quickFormat, clusterSize, ct);
            }
        } catch (Exception ex) when (ex is not OperationCanceledException) {
             _log.Error($"Format/Mount failed: {ex.Message}", ex);
             throw new FormatFailedException($"Failed to format/mount volume on disk {diskIndex}", ex);
        }
    }

    private void LockAndDismountVolumes(int diskIndex) {
        _log.Info($"Locking and dismounting volumes on disk {diskIndex}");
        var mounter = new AdvancedVolumeMounter(_log);
        var volumes = mounter.GetVolumesOnDisk(diskIndex);
        foreach (var vol in volumes) {
            _log.Info($"Dismounting {vol}...");
            try {
                using var hVol = NativeMethods.CreateFile(vol, 
                    NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, 
                    NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, 
                    IntPtr.Zero, 
                    NativeMethods.OPEN_EXISTING, 
                    0, 
                    IntPtr.Zero);
                
                if (!hVol.IsInvalid) {
                    uint bytesReturned;
                    NativeMethods.DeviceIoControl(hVol, NativeMethods.FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
                    if (!NativeMethods.DeviceIoControl(hVol, NativeMethods.FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero)) {
                        _log.Warn($"Failed to dismount volume {vol}: {Marshal.GetLastWin32Error()}");
                    }
                }
            } catch (Exception ex) {
                _log.Warn($"Error dismounting {vol}: {ex.Message}");
            }
        }
    }

    private async Task PerformDiskOperation(int diskIndex, Func<SafeFileHandle, Task> action, CancellationToken ct) {
        _log.Info($"Opening disk {diskIndex} with robust locking...");
        using var hDrive = _lockService.OpenAndLockDrive(diskIndex, writeAccess: true);
        
        if (hDrive.IsInvalid) {
            throw new InvalidOperationException($"Failed to open and lock disk {diskIndex}");
        }

        await action(hDrive);
    }

    private long GetDiskSize(SafeFileHandle hDrive) {
        var geom = new NativeMethods.DISK_GEOMETRY_EX();
        int size = Marshal.SizeOf(geom);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try {
            if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_GET_DRIVE_GEOMETRY_EX, IntPtr.Zero, 0, ptr, (uint)size, out _, IntPtr.Zero)) {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get disk geometry");
            }
            geom = Marshal.PtrToStructure<NativeMethods.DISK_GEOMETRY_EX>(ptr);
            _log.Info($"Disk size: {geom.DiskSize / (1024*1024*1024)} GB ({geom.DiskSize} bytes)");
            return geom.DiskSize;
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private void CleanDisk(SafeFileHandle hDrive) {
        _log.Info("Cleaning disk (Setting to RAW)...");
        var createDisk = new NativeMethods.CREATE_DISK {
            PartitionStyle = (int)NativeMethods.PARTITION_STYLE.RAW,
            Union = new NativeMethods.CREATE_DISK_UNION()
        };

        int size = Marshal.SizeOf(createDisk);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try {
            Marshal.StructureToPtr(createDisk, ptr, false);
            if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_CREATE_DISK, ptr, (uint)size, IntPtr.Zero, 0, out _, IntPtr.Zero)) {
                int err = Marshal.GetLastWin32Error();
                _log.Warn($"Failed to clean disk (IOCTL_DISK_CREATE_DISK RAW): {err}");
            } else {
                _log.Info("Disk cleaned successfully");
            }
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
        NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_UPDATE_PROPERTIES, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
    }

    public async Task FormatCustomAsync(int diskIndex, List<PartitionDefinition> partitions, CancellationToken ct) {
        _log.Info($"=== NATIVE CUSTOM FORMAT START === Disk {diskIndex}, Partitions: {partitions.Count}");
        long totalDiskSize = 0;

        _autoMountManager.WithAutoMountDisabled(() => {
            try {
                LockAndDismountVolumes(diskIndex);
                PerformDiskOperation(diskIndex, async (hDrive) => {
                     totalDiskSize = GetDiskSize(hDrive);
                     CleanDisk(hDrive);
                     InitializeDisk(hDrive, true); 
                     CreateCustomPartitionLayout(hDrive, partitions, totalDiskSize);
                     ForceRescan(hDrive);
                }, ct).Wait(ct);
            } catch (Exception ex) {
                _log.Error($"Disk preparation failed: {ex.Message}", ex);
                throw new FormatFailedException("Failed to prepare disk layout", ex);
            }
        });
        
        await Task.Delay(1000, ct);

        
        
        long currentOffset = DiskConstants.GptDataOffset; 

        for (int i = 0; i < partitions.Count; i++) {
            var partDef = partitions[i];
            int partitionNumber = i + 2; 
            
            try {
                _log.Info($"Processing Partition {partitionNumber}: {partDef.Label} ({partDef.FileSystem})");
                string volumePath = await WaitForVolume(diskIndex, partitionNumber, currentOffset, ct);
                
                await FormatVolumeAsync(volumePath, partDef.FileSystem, partDef.Label, true, 0, ct);
                
                if (partDef.IsBootable) {
                    
                    
                }

                
                
                
                long sizeBytes = partDef.SizeMB == 0 
                    ? (totalDiskSize - currentOffset - (1024*1024)) 
                    : (partDef.SizeMB * 1024 * 1024);
                
                
                currentOffset += sizeBytes; 

            } catch (Exception ex) {
                _log.Error($"Failed to format partition {partitionNumber}: {ex.Message}");
                
                
                throw;
            }
        }
    }

    private void CreateCustomPartitionLayout(SafeFileHandle hDrive, List<PartitionDefinition> partitions, long diskSize) {
        _log.Info($"Creating custom GPT partition layout with {partitions.Count} user partitions");
        
        var layout = new NativeMethods.DRIVE_LAYOUT_INFORMATION_EX {
            PartitionStyle = (uint)NativeMethods.PARTITION_STYLE.GPT,
            PartitionCount = (uint)(partitions.Count + 1), 
            Union = new NativeMethods.DRIVE_LAYOUT_INFORMATION_UNION()
        };

        layout.Union.Gpt = new NativeMethods.DRIVE_LAYOUT_INFORMATION_GPT {
            DiskId = Guid.NewGuid(),
            StartingUsableOffset = 34 * 512, 
            UsableLength = diskSize - (34 * 512) * 2,
            MaxPartitionCount = 128
        };

        var nativePartitions = new NativeMethods.PARTITION_INFORMATION_EX[partitions.Count + 1];

        
        nativePartitions[0] = new NativeMethods.PARTITION_INFORMATION_EX {
            PartitionStyle = (uint)NativeMethods.PARTITION_STYLE.GPT,
            StartingOffset = 1024 * 1024, 
            PartitionLength = 16 * 1024 * 1024, 
            PartitionNumber = 1,
            RewritePartition = true,
            Union = new NativeMethods.PARTITION_INFORMATION_UNION {
                Gpt = new NativeMethods.PARTITION_INFORMATION_GPT {
                    PartitionType = new Guid("E3C9E316-0B5C-4DB8-817D-F92DF00215AE"), 
                    PartitionId = Guid.NewGuid(),
                    Attributes = 0
                }
            }
        };
        nativePartitions[0].Union.Gpt.SetName("Microsoft Reserved Partition");

        
        long currentOffset = nativePartitions[0].StartingOffset + nativePartitions[0].PartitionLength;
        
        for (int i = 0; i < partitions.Count; i++) {
            var def = partitions[i];
            long sizeBytes = def.SizeMB * 1024 * 1024;
            
            if (def.SizeMB == 0 || i == partitions.Count - 1 && def.SizeMB == 0) {
                
                
                sizeBytes = diskSize - currentOffset - (1024 * 1024);
            }

            nativePartitions[i + 1] = new NativeMethods.PARTITION_INFORMATION_EX {
                PartitionStyle = (uint)NativeMethods.PARTITION_STYLE.GPT,
                StartingOffset = currentOffset,
                PartitionLength = sizeBytes,
                PartitionNumber = (uint)(i + 2),
                RewritePartition = true,
                Union = new NativeMethods.PARTITION_INFORMATION_UNION {
                    Gpt = new NativeMethods.PARTITION_INFORMATION_GPT {
                        PartitionType = new Guid("EBD0A0A2-B9E5-4433-87C0-68B6B72699C7"), 
                        PartitionId = Guid.NewGuid(),
                        Attributes = 0
                    }
                }
            };
            nativePartitions[i + 1].Union.Gpt.SetName(def.Label);
            
            currentOffset += sizeBytes;
        }

        SetDriveLayout(hDrive, layout, nativePartitions);
    }

    private void InitializeDisk(SafeFileHandle hDrive, bool useGPT) {
        _log.Info($"Initializing disk (GPT={useGPT})...");
        var createDisk = new NativeMethods.CREATE_DISK {
            PartitionStyle = useGPT ? (int)NativeMethods.PARTITION_STYLE.GPT : (int)NativeMethods.PARTITION_STYLE.MBR,
            Union = new NativeMethods.CREATE_DISK_UNION()
        };

        if (useGPT) {
            createDisk.Union.Gpt = new NativeMethods.CREATE_DISK_GPT {
                DiskId = Guid.NewGuid(),
                MaxPartitionCount = 128
            };
        } else {
            createDisk.Union.Mbr = new NativeMethods.CREATE_DISK_MBR {
                Signature = (uint)new Random().Next()
            };
        }

        int size = Marshal.SizeOf(createDisk);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try {
            Marshal.StructureToPtr(createDisk, ptr, false);
            if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_CREATE_DISK, ptr, (uint)size, IntPtr.Zero, 0, out _, IntPtr.Zero)) {
                int err = Marshal.GetLastWin32Error();
                _log.Warn($"IOCTL_DISK_CREATE_DISK returned error {err}");
            } else {
                _log.Info("Disk initialized successfully");
            }
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
        NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_UPDATE_PROPERTIES, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
    }

    private void CreateSinglePartition(SafeFileHandle hDrive, bool useGPT, long diskSize) {
        _log.Info($"Creating partition layout: {(useGPT ? "GPT" : "MBR")}");
        var layout = new NativeMethods.DRIVE_LAYOUT_INFORMATION_EX {
            PartitionStyle = useGPT ? (uint)NativeMethods.PARTITION_STYLE.GPT : (uint)NativeMethods.PARTITION_STYLE.MBR,
            PartitionCount = useGPT ? 2u : 1u,
            Union = new NativeMethods.DRIVE_LAYOUT_INFORMATION_UNION()
        };

        if (useGPT) {
            layout.Union.Gpt = new NativeMethods.DRIVE_LAYOUT_INFORMATION_GPT {
                DiskId = Guid.NewGuid(),
                StartingUsableOffset = 34 * 512, 
                UsableLength = diskSize - (34 * 512) * 2,
                MaxPartitionCount = 128
            };

            var partitions = new NativeMethods.PARTITION_INFORMATION_EX[2];
            var msrPartition = new NativeMethods.PARTITION_INFORMATION_EX {
                PartitionStyle = (uint)NativeMethods.PARTITION_STYLE.GPT,
                StartingOffset = 1024 * 1024, 
                PartitionLength = 16 * 1024 * 1024, 
                PartitionNumber = 1,
                RewritePartition = true,
                Union = new NativeMethods.PARTITION_INFORMATION_UNION {
                    Gpt = new NativeMethods.PARTITION_INFORMATION_GPT {
                        PartitionType = new Guid("E3C9E316-0B5C-4DB8-817D-F92DF00215AE"), 
                        PartitionId = Guid.NewGuid(),
                        Attributes = 0
                    }
                }
            };
            msrPartition.Union.Gpt.SetName("Microsoft Reserved Partition");
            partitions[0] = msrPartition;

            var dataPartition = new NativeMethods.PARTITION_INFORMATION_EX {
                PartitionStyle = (uint)NativeMethods.PARTITION_STYLE.GPT,
                StartingOffset = DiskConstants.GptDataOffset,
                PartitionLength = diskSize - DiskConstants.GptDataOffset - (1024*1024), 
                PartitionNumber = 2,
                RewritePartition = true,
                Union = new NativeMethods.PARTITION_INFORMATION_UNION {
                    Gpt = new NativeMethods.PARTITION_INFORMATION_GPT {
                        PartitionType = new Guid("EBD0A0A2-B9E5-4433-87C0-68B6B72699C7"), 
                        PartitionId = Guid.NewGuid(),
                        Attributes = 0
                    }
                }
            };
            dataPartition.Union.Gpt.SetName("Basic Data Partition");
            partitions[1] = dataPartition;
            SetDriveLayout(hDrive, layout, partitions);
        } else {
            layout.Union.Mbr = new NativeMethods.DRIVE_LAYOUT_INFORMATION_MBR {
                Signature = (uint)new Random().Next()
            };

            var partitions = new NativeMethods.PARTITION_INFORMATION_EX[1];
            partitions[0] = new NativeMethods.PARTITION_INFORMATION_EX {
                PartitionStyle = (uint)NativeMethods.PARTITION_STYLE.MBR,
                StartingOffset = DiskConstants.MbrDataOffset,
                PartitionLength = diskSize - DiskConstants.MbrDataOffset - (1024*1024),
                PartitionNumber = 1,
                RewritePartition = true,
                Union = new NativeMethods.PARTITION_INFORMATION_UNION {
                    Mbr = new NativeMethods.PARTITION_INFORMATION_MBR {
                        PartitionType = 0x07, 
                        BootIndicator = true,
                        RecognizedPartition = true,
                        HiddenSectors = (uint)(DiskConstants.MbrDataOffset / 512)
                    }
                }
            };
            SetDriveLayout(hDrive, layout, partitions);
        }
    }

    private void SetDriveLayout(SafeFileHandle hDrive, NativeMethods.DRIVE_LAYOUT_INFORMATION_EX layout, NativeMethods.PARTITION_INFORMATION_EX[] partitions) {
        int headerSize = Marshal.SizeOf(typeof(NativeMethods.DRIVE_LAYOUT_INFORMATION_EX));
        int partitionSize = Marshal.SizeOf(typeof(NativeMethods.PARTITION_INFORMATION_EX));
        int totalSize = headerSize + (partitions.Length > 0 ? (partitions.Length - 1) * partitionSize : 0);

        _log.Info($"Structure sizes: Header={headerSize}, Partition={partitionSize}");
        _log.Info($"Setting drive layout: TotalSize={totalSize}, PartitionCount={partitions.Length}");

        layout.PartitionCount = (uint)partitions.Length;
        layout.PartitionEntry = new NativeMethods.PARTITION_INFORMATION_EX[1];
        if (partitions.Length > 0) {
            layout.PartitionEntry[0] = partitions[0];
        }

        IntPtr ptr = Marshal.AllocHGlobal(totalSize);
        try {
            for (int i = 0; i < totalSize; i++) {
                Marshal.WriteByte(ptr, i, 0);
            }
            Marshal.StructureToPtr(layout, ptr, false);
            for (int i = 1; i < partitions.Length; i++) {
                IntPtr partitionPtr = new IntPtr(ptr.ToInt64() + headerSize + (i - 1) * partitionSize);
                Marshal.StructureToPtr(partitions[i], partitionPtr, false);
            }

            if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_SET_DRIVE_LAYOUT_EX, ptr, (uint)totalSize, IntPtr.Zero, 0, out _, IntPtr.Zero)) {
                int error = Marshal.GetLastWin32Error();
                _log.Error($"SetDriveLayout failed with Win32 error {error}");
                throw new Win32Exception(error, "Failed to set drive layout");
            }
            _log.Info("Drive layout set successfully");
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
        NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_UPDATE_PROPERTIES, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
    }

    private void ForceRescan(SafeFileHandle hDrive) {
        _log.Info("Step 4: Updating disk properties (Force Rescan)");
        if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_UPDATE_PROPERTIES, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero)) {
            int err = Marshal.GetLastWin32Error();
            _log.Warn($"IOCTL_DISK_UPDATE_PROPERTIES returned error {err} (ignoring)");
        }
        
        int size = 4096; 
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try {
            if (!NativeMethods.DeviceIoControl(hDrive, NativeMethods.IOCTL_DISK_GET_DRIVE_LAYOUT_EX, IntPtr.Zero, 0, ptr, (uint)size, out _, IntPtr.Zero)) {
                _log.Warn($"IOCTL_DISK_GET_DRIVE_LAYOUT_EX failed during rescan (ignoring): {Marshal.GetLastWin32Error()}");
            } else {
                _log.Info("Rescan: Successfully read back drive layout");
            }
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private async Task<string> WaitForVolume(int diskIndex, int partitionNumber, long partitionOffset, CancellationToken ct) {
        _log.Info($"Waiting for volume on disk {diskIndex} partition {partitionNumber} (offset {partitionOffset})...");
        var mounter = new AdvancedVolumeMounter(_log);

        await mounter.TriggerMultipleRefreshMethods(diskIndex, ct);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(45);
        int attempt = 0;
        bool triedAssign = false;
        
        while (sw.Elapsed < timeout) {
            ct.ThrowIfCancellationRequested();
            
            string? volumePath = mounter.GetVolumeForPartition(diskIndex, partitionOffset, false);
            if (volumePath != null) {
                _log.Info($"Found volume: {volumePath}");
                return volumePath;
            }
            
            if (sw.Elapsed.TotalSeconds > 5 && !triedAssign) {
                _log.Warn("Volume not appearing after 5s. Forcing drive letter assignment via diskpart...");
                try {
                    await AssignDriveLetterAsync(diskIndex, partitionNumber, ct);
                    await mounter.TriggerMultipleRefreshMethods(diskIndex, ct);
                } catch (Exception ex) {
                    _log.Warn($"Failed to force assign letter: {ex.Message}");
                }
                triedAssign = true;
            }

            attempt++;
            int delay = Math.Min(2000, 100 * (1 << Math.Min(attempt, 6)));
            await Task.Delay(delay, ct);
        }
        throw new MountFailedException($"Volume did not appear after 45 seconds on disk {diskIndex} partition {partitionNumber}");
    }

    private async Task FormatVolumeAsync(string volumePath, string fileSystem, string label, bool quickFormat, int clusterSize, CancellationToken ct) {
        _log.Info($"Step 5: Formatting volume {volumePath} as {fileSystem}, Label: {label}");
        string driveLetter = volumePath.Replace(@"\\.\", "") + "\\";
        
        if (fileSystem.Equals("ext4", StringComparison.OrdinalIgnoreCase)) {
            _log.Warn($"Ext4 formatting requested for {volumePath}. Windows format.com does not support Ext4. Skipping format (Partition created but unformatted).");
            return;
        }

        await Task.Run(() => {
             var psi = new System.Diagnostics.ProcessStartInfo {
                 FileName = "format.com",
                 Arguments = $"{driveLetter.TrimEnd('\\')} /FS:{fileSystem} /V:{label} /Q /Y",
                 UseShellExecute = false,
                 CreateNoWindow = true,
                 RedirectStandardOutput = true,
                 RedirectStandardError = true
             };
             if (clusterSize > 0) {
                 psi.Arguments += $" /A:{clusterSize}";
             }
             _log.Info($"Executing: format.com {psi.Arguments}");
             using var p = System.Diagnostics.Process.Start(psi);
             if (p == null) {
                 throw new FormatFailedException("Failed to start format.com process");
             }
             p.WaitForExit();
             if (p.ExitCode != 0) {
                 string err = p.StandardError.ReadToEnd();
                 string outStr = p.StandardOutput.ReadToEnd();
                 _log.Error($"Format failed: {err}\n{outStr}");
                 throw new FormatFailedException($"Format failed with exit code {p.ExitCode}");
             }
        }, ct);
    }

    private async Task ForceVolumeOnlineAndAssignLetter(int diskIndex, int partitionNumber, CancellationToken ct) {
        _log.Info("=== FORCING VOLUME ONLINE AND DRIVE LETTER ASSIGNMENT ===");
        await Task.Delay(VolumeMountDelay, ct);
        var vdsRefresher = new Vds.VdsPartitionRefresher(_log);
        await vdsRefresher.RefreshPartitionsAsync(ct);
        await Task.Delay(1000, ct);
        
        await AssignDriveLetterAsync(diskIndex, partitionNumber, ct);
        
        await Task.Delay(2000, ct);
        _log.Info("=== VOLUME ASSIGNMENT COMPLETE ===");
    }

    private async Task AssignDriveLetterAsync(int diskIndex, int partitionNumber, CancellationToken ct) {
        _log.Info($"Attempting automatic drive letter assignment via diskpart for Disk {diskIndex} Partition {partitionNumber}...");
        var psi = new System.Diagnostics.ProcessStartInfo {
            FileName = "diskpart.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc != null) {
            await proc.StandardInput.WriteLineAsync("rescan");
            await proc.StandardInput.WriteLineAsync($"select disk {diskIndex}");
            await proc.StandardInput.WriteLineAsync($"select partition {partitionNumber}");
            await proc.StandardInput.WriteLineAsync("assign");
            await proc.StandardInput.WriteLineAsync("exit");
            proc.StandardInput.Close();
            await proc.WaitForExitAsync(ct);
            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            _log.Info($"Diskpart output: {output}");
            if (proc.ExitCode == 0) {
                _log.Info("Drive letter assigned successfully via diskpart");
            } else {
                _log.Warn($"Diskpart exited with code {proc.ExitCode}");
            }
        }
    }
}



