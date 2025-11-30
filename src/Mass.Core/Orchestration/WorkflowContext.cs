namespace Mass.Core.Orchestration;

public class WorkflowContext
{
    public Dictionary<string, object> Variables { get; } = new();
    public Dictionary<string, object> StepResults { get; } = new();
    public List<string> Logs { get; } = new();
    public CancellationToken CancellationToken { get; set; }

    public void Log(string message)
    {
        Logs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    public void SetVariable(string name, object value)
    {
        Variables[name] = value;
    }

    public object? GetVariable(string name)
    {
        return Variables.TryGetValue(name, out var value) ? value : null;
    }

    public void SetStepResult(string stepId, object result)
    {
        StepResults[stepId] = result;
    }

    public object? GetStepResult(string stepId)
    {
        return StepResults.TryGetValue(stepId, out var result) ? result : null;
    }

    public string InterpolateString(string input)
    {
        var result = input;
        
        foreach (var kvp in Variables)
        {
            result = result.Replace($"${{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }
        
        return result;
    }
}
