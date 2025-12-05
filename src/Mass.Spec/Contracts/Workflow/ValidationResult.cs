namespace Mass.Spec.Contracts.Workflow;

/// <summary>
/// Represents the result of a workflow validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the workflow definition is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
