namespace Mass.Spec.Contracts.Config;

/// <summary>
/// Configuration settings for USB operations.
/// </summary>
public class UsbSettings
{
    /// <summary>
    /// Whether to enable safe mode (prevents writing to system drives).
    /// </summary>
    public bool SafeMode { get; set; } = true;

    /// <summary>
    /// The default volume label to use if none is specified.
    /// </summary>
    public string DefaultLabel { get; set; } = "MASS_BOOT";

    /// <summary>
    /// Whether to verify the burn after writing.
    /// </summary>
    public bool VerifyAfterBurn { get; set; } = true;

    /// <summary>
    /// Whether to eject the device after completion.
    /// </summary>
    public bool EjectAfterBurn { get; set; } = false;

    /// <summary>
    /// Default partition scheme (GPT, MBR).
    /// </summary>
    public string DefaultPartitionScheme { get; set; } = "GPT";
}
