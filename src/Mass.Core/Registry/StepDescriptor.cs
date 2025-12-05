namespace Mass.Core.Registry;

/// <summary>
/// Describes a registered workflow step handler.
/// </summary>
public class StepDescriptor
{
    /// <summary>
    /// Unique identifier for the step type (e.g., "burn.iso", "file.copy").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Version of the step handler.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Fully qualified type name of the handler (for DI resolution).
    /// </summary>
    public string HandlerTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Required permissions for this step.
    /// </summary>
    public string[] Permissions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Human-readable description of the step.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
