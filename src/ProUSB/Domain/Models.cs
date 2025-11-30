using System;
using System.Collections.Generic;
namespace ProUSB.Domain;

public record IsoMetadata {
    public string FilePath { get; init; } = "";
    public string FileName { get; init; } = "";
    public long FileSize { get; init; }
}

public record UsbDeviceInfo {
    public string DeviceId { get; init; } = "";
    public int PhysicalIndex { get; init; }
    public string FriendlyName { get; init; } = "";
    public string BusType { get; init; } = "Unknown";
    public long TotalSize { get; init; }
    public List<string> MountPoints { get; init; } = new();
    public ushort VendorId { get; init; }
    public ushort ProductId { get; init; }
}

public record PartitionDefinition {
    public string Label { get; init; } = "New Volume";
    public string FileSystem { get; init; } = "FAT32"; 
    public long SizeMB { get; init; } 
    public bool IsBootable { get; init; }
}

public record DeploymentConfiguration {
    public required string JobName { get; init; }
    public required IsoMetadata SourceIso { get; init; }
    public required UsbDeviceInfo TargetDevice { get; init; }
    public BurnStrategy Strategy { get; init; }
    public string VolumeLabel { get; init; } = "BOOT";
    public string FileSystem { get; init; } = "fat32";
    public string PartitionScheme { get; init; } = "gpt";
    public int ClusterSize { get; init; } = 0;
    public bool QuickFormat { get; init; } = true;
    public bool BypassWin11 { get; init; }
    public int PersistenceSize { get; init; } = 0; 
    public List<PartitionDefinition>? CustomLayout { get; init; }
}

public struct WriteStatistics {
    public long BytesWritten;
    public double PercentComplete;
    public string Message;
    public double SpeedMBps;
    public TimeSpan TimeElapsed;
    public TimeSpan TimeRemaining;
}

public record BurnJob {
    public required UsbDeviceInfo Device { get; init; }
    public required string IsoPath { get; init; }
    public required DeploymentConfiguration Config { get; init; }
    public string Status { get; set; } = "Pending";
    public double Progress { get; set; } = 0;
}

