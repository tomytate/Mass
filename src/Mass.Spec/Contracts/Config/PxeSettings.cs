namespace Mass.Spec.Contracts.Config;

/// <summary>
/// Configuration settings for the PXE server.
/// </summary>
public class PxeSettings
{
    /// <summary>
    /// The root directory for TFTP files.
    /// </summary>
    public string TftpRoot { get; set; } = "tftpboot";

    /// <summary>
    /// The IP address to bind the DHCP server to.
    /// </summary>
    public string DhcpBindAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// The starting IP address for the DHCP pool.
    /// </summary>
    public string DhcpPoolStart { get; set; } = "192.168.1.100";

    /// <summary>
    /// The ending IP address for the DHCP pool.
    /// </summary>
    public string DhcpPoolEnd { get; set; } = "192.168.1.200";

    /// <summary>
    /// The port for the TFTP server.
    /// </summary>
    public int TftpPort { get; set; } = 69;

    /// <summary>
    /// The port for the HTTP server.
    /// </summary>
    public int HttpPort { get; set; } = 8080;

    /// <summary>
    /// Whether to enable the DHCP server.
    /// </summary>
    public bool EnableDhcp { get; set; } = true;
}
