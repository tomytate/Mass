namespace Mass.Spec.Contracts.Usb;

/// <summary>
/// Represents the result of a verification operation.
/// </summary>
public class VerifyResult
{
    /// <summary>
    /// Whether the verification was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The number of mismatched bytes found.
    /// </summary>
    public long MismatchedBytes { get; set; }

    /// <summary>
    /// List of verification errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
