namespace Mass.Spec.Contracts.Pxe;

/// <summary>
/// Describes a boot file available on the PXE server.
/// </summary>
public class PxeBootFileDescriptor
{
    /// <summary>
    /// Unique identifier for the file.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The architecture supported by this file (e.g., x64, arm64).
    /// </summary>
    public string Architecture { get; set; } = "x64";

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The relative path to the file on the server.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
}
