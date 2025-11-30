namespace Mass.Core.Orchestration;

public class WorkflowDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<WorkflowStep> Steps { get; set; } = new();
}
