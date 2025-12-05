namespace Mass.Spec.Contracts.Usb;

/// <summary>
/// Represents a job to burn an image to a USB device.
/// </summary>
public class UsbJob
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Path to the source image file (ISO, IMG, etc.).
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// The target device identifier (e.g., PhysicalDrive1).
    /// </summary>
    public string TargetDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The label to assign to the volume.
    /// </summary>
    public string VolumeLabel { get; set; } = "MASS_BOOT";

    /// <summary>
    /// Whether to verify the burn after writing.
    /// </summary>
    public bool Verify { get; set; }

    /// <summary>
    /// Whether to eject the device after completion.
    /// </summary>
    public bool Eject { get; set; }

    /// <summary>
    /// Partition scheme to use (GPT, MBR).
    /// </summary>
    public string PartitionScheme { get; set; } = "GPT";

    /// <summary>
    /// File system to format with (FAT32, NTFS).
    /// </summary>
    public string FileSystem { get; set; } = "FAT32";

    /// <summary>
    /// Size of persistence partition in MB (0 for none).
    /// </summary>
    public int PersistenceSizeMB { get; set; } = 0;
}
