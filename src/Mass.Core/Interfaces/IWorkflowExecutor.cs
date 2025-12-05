using Mass.Spec.Contracts.Workflow;

namespace Mass.Core.Interfaces;

/// <summary>
/// Public facade for workflow execution.
/// </summary>
public interface IWorkflowExecutor
{
    /// <summary>
    /// Executes a workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the workflow execution.</returns>
    Task<WorkflowResult> ExecuteAsync(WorkflowDefinition workflow, WorkflowExecutionOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Validates a workflow definition without executing it.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateAsync(WorkflowDefinition workflow, CancellationToken ct = default);
}
