namespace Mass.Spec.Contracts.Usb;

/// <summary>
/// Represents information about a USB device.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// The unique identifier of the device.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The friendly name of the device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The total size of the device in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The path to the device (e.g., \\.\PhysicalDrive1).
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Whether the device is removable.
    /// </summary>
    public bool IsRemovable { get; set; }
}
