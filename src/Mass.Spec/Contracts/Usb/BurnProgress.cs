namespace Mass.Spec.Contracts.Usb;

/// <summary>
/// Represents the progress of a burn operation.
/// </summary>
public class BurnProgress
{
    /// <summary>
    /// The percentage of completion (0-100).
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// The current write speed in bytes per second.
    /// </summary>
    public double SpeedBytesPerSecond { get; set; }

    /// <summary>
    /// The estimated time remaining.
    /// </summary>
    public TimeSpan? Eta { get; set; }

    /// <summary>
    /// Description of the current operation being performed.
    /// </summary>
    public string CurrentOperation { get; set; } = string.Empty;

    /// <summary>
    /// Number of bytes processed so far.
    /// </summary>
    public long BytesProcessed { get; set; }

    /// <summary>
    /// Total number of bytes to process.
    /// </summary>
    public long TotalBytes { get; set; }
}
