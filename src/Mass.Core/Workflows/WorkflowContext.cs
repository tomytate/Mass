namespace Mass.Core.Workflows;

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

    private static readonly System.Buffers.SearchValues<char> _searchValues = System.Buffers.SearchValues.Create("$");

    public string InterpolateString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        if (input.AsSpan().IndexOfAny(_searchValues) == -1)
            return input;

        var sb = new System.Text.StringBuilder(input.Length);
        var span = input.AsSpan();
        
        while (true)
        {
            var index = span.IndexOfAny(_searchValues);
            if (index == -1)
            {
                sb.Append(span);
                break;
            }

            sb.Append(span[..index]);
            span = span[index..];

            if (span.Length >= 2 && span[1] == '{')
            {
                var end = span.IndexOf('}');
                if (end != -1)
                {
                    var key = span.Slice(2, end - 2).ToString();
                    if (Variables.TryGetValue(key, out var val))
                    {
                        sb.Append(val?.ToString() ?? string.Empty);
                        span = span[(end + 1)..];
                        continue;
                    }
                }
            }
            
            sb.Append(span[0]);
            span = span[1..];
        }
        
        return sb.ToString();
    }
}
