namespace Mass.Spec.Config;

/// <summary>
/// Root configuration object for Mass Suite.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// General application settings.
    /// </summary>
    public GeneralSettings General { get; set; } = new();

    /// <summary>
    /// PXE server settings.
    /// </summary>
    public PxeSettings Pxe { get; set; } = new();

    /// <summary>
    /// USB burner settings.
    /// </summary>
    /// <summary>
    /// USB burner settings.
    /// </summary>
    public UsbSettings Usb { get; set; } = new();

    /// <summary>
    /// Telemetry settings.
    /// </summary>
    public TelemetrySettings Telemetry { get; set; } = new();
}

/// <summary>
/// Telemetry settings.
/// </summary>
public class TelemetrySettings
{
    /// <summary>
    /// Whether telemetry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether the user has made a consent decision.
    /// </summary>
    public bool ConsentDecisionMade { get; set; }
}

/// <summary>
/// General settings.
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// Application language code (e.g., en-US).
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// UI Theme (System, Light, Dark).
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Whether to check for updates on startup.
    /// </summary>
    public bool CheckForUpdates { get; set; } = true;
}

/// <summary>
/// PXE server settings.
/// </summary>
public class PxeSettings
{
    /// <summary>
    /// Root directory for PXE boot files.
    /// </summary>
    public string TftpRoot { get; set; } = "pxe_root";

    /// <summary>
    /// Port for TFTP server.
    /// </summary>
    public int TftpPort { get; set; } = 69;

    /// <summary>
    /// Port for HTTP boot server.
    /// </summary>
    public int HttpPort { get; set; } = 8080;

    /// <summary>
    /// Whether to enable the built-in DHCP server.
    /// </summary>
    public bool EnableDhcp { get; set; } = true;
}

/// <summary>
/// USB burner settings.
/// </summary>
public class UsbSettings
{
    /// <summary>
    /// Whether to verify writes after burning.
    /// </summary>
    public bool VerifyWrites { get; set; } = true;

    /// <summary>
    /// Whether to eject the device after burning.
    /// </summary>
    public bool EjectAfterBurn { get; set; } = true;
}
