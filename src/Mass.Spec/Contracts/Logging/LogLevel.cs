namespace Mass.Spec.Contracts.Logging;

/// <summary>
/// Defines the severity levels for logging.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Very detailed trace information.
    /// </summary>
    Trace,

    /// <summary>
    /// Detailed debug information.
    /// </summary>
    Debug,

    /// <summary>
    /// General informational messages.
    /// </summary>
    Information,

    /// <summary>
    /// Warning messages for potential issues.
    /// </summary>
    Warning,

    /// <summary>
    /// Error messages for failed operations.
    /// </summary>
    Error,

    /// <summary>
    /// Critical errors that cause system failure.
    /// </summary>
    Critical
}
