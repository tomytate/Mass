using Mass.Spec.Exceptions;

namespace Mass.Spec.Contracts.Workflow;

public class WorkflowResult
{
    public bool Success { get; set; }
    public Dictionary<string, object> Outputs { get; set; } = new();
    public List<WorkflowStepResult> CompletedSteps { get; set; } = new();
    public ErrorCode? Error { get; set; }
}
