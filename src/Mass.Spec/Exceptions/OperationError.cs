namespace Mass.Spec.Exceptions;

/// <summary>
/// Represents a structured error code and message.
/// </summary>
public class OperationError
{
    /// <summary>
    /// The unique error code (e.g., "USB_WRITE_FAILED").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation can be retried.
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Suggested action for the user.
    /// </summary>
    public string? SuggestedAction { get; set; }
}
