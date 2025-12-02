namespace Mass.Core.Workflows;

public interface IWorkflowEngine
{
    Task<WorkflowResult> RunWorkflowAsync(string workflowId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<WorkflowValidationResult> ValidateWorkflowAsync(string workflowId);
    IEnumerable<WorkflowDefinition> GetAvailableWorkflows();
}

public record WorkflowValidationResult(bool IsValid, IEnumerable<string> Errors);
