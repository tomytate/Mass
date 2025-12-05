namespace Mass.Spec.Contracts.Config;

/// <summary>
/// Root configuration object for the application.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Configuration for PXE services.
    /// </summary>
    public PxeSettings Pxe { get; set; } = new();

    /// <summary>
    /// Configuration for USB operations.
    /// </summary>
    public UsbSettings Usb { get; set; } = new();

    /// <summary>
    /// The directory where plugins are stored.
    /// </summary>
    public string PluginsDirectory { get; set; } = "plugins";

    /// <summary>
    /// The minimum log level to record.
    /// </summary>
    public string MinLogLevel { get; set; } = "Information";
}
