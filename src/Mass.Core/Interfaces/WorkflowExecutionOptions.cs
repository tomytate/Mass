namespace Mass.Core.Interfaces;

/// <summary>
/// Options for workflow execution.
/// </summary>
public class WorkflowExecutionOptions
{
    /// <summary>
    /// Whether to enable automatic retry on step failures.
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum number of retries per step.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether to stop execution on the first step failure.
    /// </summary>
    public bool StopOnFirstFailure { get; set; } = true;

    /// <summary>
    /// Additional context to pass to the workflow execution.
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}
