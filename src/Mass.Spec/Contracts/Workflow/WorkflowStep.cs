namespace Mass.Spec.Contracts.Workflow;

/// <summary>
/// Represents a single step in a workflow.
/// </summary>
public class WorkflowStep
{
    /// <summary>
    /// Unique identifier for the step within the workflow.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of action this step performs (e.g., "burn.iso", "file.copy").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the step.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Configuration parameters for the step action.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Condition that must be true for the step to execute.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Maximum number of retries if the step fails.
    /// </summary>
    public int MaxRetries { get; set; } = 0;

    /// <summary>
    /// Delay in milliseconds between retries.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether this step should always run regardless of previous failures.
    /// </summary>
    public bool RunAlways { get; set; } = false;
}
