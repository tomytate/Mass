namespace Mass.Spec.Contracts.Usb;

public class DeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsRemovable { get; set; }
}
