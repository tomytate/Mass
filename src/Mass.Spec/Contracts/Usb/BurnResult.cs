namespace Mass.Spec.Contracts.Usb;

/// <summary>
/// Represents the result of a burn operation.
/// </summary>
public class BurnResult
{
    /// <summary>
    /// The ID of the job that produced this result.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the burn was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the burn failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The time taken to complete the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
