namespace Mass.Core.Workflows;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly WorkflowParser _parser;
    private readonly WorkflowExecutor _executor;
    private readonly string _workflowsPath;

    public WorkflowEngine(string workflowsPath)
    {
        _workflowsPath = workflowsPath;
        _parser = new WorkflowParser();
        _executor = new WorkflowExecutor();
    }

    public async Task<WorkflowResult> RunWorkflowAsync(string workflowId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var workflow = LoadWorkflow(workflowId);
        if (workflow == null)
            return new WorkflowResult { Success = false, Message = $"Workflow '{workflowId}' not found" };

        foreach (var param in parameters)
        {
            workflow.Parameters[param.Key] = param.Value;
        }

        return await _executor.ExecuteAsync(workflow, cancellationToken);
    }

    public Task<WorkflowValidationResult> ValidateWorkflowAsync(string workflowId)
    {
        var workflow = LoadWorkflow(workflowId);
        if (workflow == null)
            return Task.FromResult(new WorkflowValidationResult(false, new[] { $"Workflow '{workflowId}' not found" }));

        var errors = new List<string>();
        if (string.IsNullOrEmpty(workflow.Name)) errors.Add("Workflow name is missing");
        if (workflow.Steps.Count == 0) errors.Add("Workflow has no steps");

        return Task.FromResult(new WorkflowValidationResult(errors.Count == 0, errors));
    }

    public IEnumerable<WorkflowDefinition> GetAvailableWorkflows()
    {
        if (!Directory.Exists(_workflowsPath))
            yield break;

        foreach (var file in Directory.GetFiles(_workflowsPath, "*.yaml").Concat(Directory.GetFiles(_workflowsPath, "*.json")))
        {
            WorkflowDefinition? workflow = null;
            try
            {
                workflow = _parser.ParseFromFile(file);
            }
            catch
            {
            }

            if (workflow != null)
                yield return workflow;
        }
    }

    private WorkflowDefinition? LoadWorkflow(string workflowId)
    {
        var path = Path.Combine(_workflowsPath, workflowId);
        if (!File.Exists(path))
        {
            path = Path.Combine(_workflowsPath, workflowId + ".yaml");
            if (!File.Exists(path))
            {
                path = Path.Combine(_workflowsPath, workflowId + ".json");
                if (!File.Exists(path)) return null;
            }
        }

        try
        {
            return _parser.ParseFromFile(path);
        }
        catch
        {
            return null;
        }
    }
}
