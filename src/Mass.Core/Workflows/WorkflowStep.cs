namespace Mass.Core.Workflows;

public abstract class WorkflowStep
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Condition { get; set; }
    public int MaxRetries { get; set; } = 0;
    public int RetryDelayMs { get; set; } = 1000;
    public bool RunAlways { get; set; } = false;
}

public class CommandStep : WorkflowStep
{
    public CommandStep()
    {
        Type = "Command";
    }
}

public class HttpRequestStep : WorkflowStep
{
    public HttpRequestStep()
    {
        Type = "HttpRequest";
    }
}

public class ScriptStep : WorkflowStep
{
    public ScriptStep()
    {
        Type = "Script";
    }
}

public class PluginStep : WorkflowStep
{
    public PluginStep()
    {
        Type = "Plugin";
    }
}

public class ServiceStep : WorkflowStep
{
    public ServiceStep()
    {
        Type = "Service";
    }
}
