namespace Mass.Spec.Contracts.Workflow;

public class WorkflowStepResult
{
    public string StepId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
}
