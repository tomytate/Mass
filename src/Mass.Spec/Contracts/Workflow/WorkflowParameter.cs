namespace Mass.Spec.Contracts.Workflow;

/// <summary>
/// Defines an input parameter for a workflow.
/// </summary>
public class WorkflowParameter
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The data type of the parameter (e.g., "string", "bool", "int").
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Default value if not provided.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; }
}
