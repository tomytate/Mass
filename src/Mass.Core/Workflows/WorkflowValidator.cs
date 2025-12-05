using Mass.Spec.Contracts.Workflow;

namespace Mass.Core.Workflows;

public class WorkflowValidator
{
    public ValidationResult Validate(WorkflowDefinition workflow)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(workflow.Id))
        {
            result.IsValid = false;
            result.Errors.Add("Workflow ID is required");
        }

        if (string.IsNullOrEmpty(workflow.Name))
        {
            result.IsValid = false;
            result.Errors.Add("Workflow Name is required");
        }

        if (workflow.Steps.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Workflow must have at least one step");
        }

        var stepIds = new HashSet<string>();
        foreach (var step in workflow.Steps)
        {
            if (string.IsNullOrEmpty(step.Id))
            {
                result.IsValid = false;
                result.Errors.Add($"Step '{step.Name}' is missing an ID");
            }
            else if (!stepIds.Add(step.Id))
            {
                result.IsValid = false;
                result.Errors.Add($"Duplicate step ID found: {step.Id}");
            }

            if (string.IsNullOrEmpty(step.Action))
            {
                result.IsValid = false;
                result.Errors.Add($"Step '{step.Name}' is missing an Action");
            }
        }

        return result;
    }
}
