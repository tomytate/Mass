namespace Mass.Spec.Contracts.Logging;

/// <summary>
/// Represents a single log entry.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// The timestamp of the log entry.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The severity level of the log.
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// The message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The source context (e.g., class name or component).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Optional exception details.
    /// </summary>
    public string? Exception { get; set; }
}
