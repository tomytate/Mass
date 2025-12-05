namespace Mass.Spec.Contracts.Workflow;

/// <summary>
/// Defines the structure of a workflow.
/// </summary>
public class WorkflowDefinition
{
    /// <summary>
    /// Unique identifier for the workflow.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the workflow.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version of the workflow definition.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Description of what the workflow does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of steps to execute.
    /// </summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    /// Input parameters required by the workflow.
    /// </summary>
    public List<WorkflowParameter> Parameters { get; set; } = new();

    /// <summary>
    /// Runtime parameter values (key-value pairs).
    /// </summary>
    public Dictionary<string, object> ParameterValues { get; set; } = new();
}
