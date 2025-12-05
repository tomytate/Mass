using Mass.Spec.Contracts.Workflow;

namespace Mass.Core.Workflows;

public class CommandStep : WorkflowStep
{
    public CommandStep()
    {
        Action = "Command";
    }
}

public class HttpRequestStep : WorkflowStep
{
    public HttpRequestStep()
    {
        Action = "HttpRequest";
    }
}

public class ScriptStep : WorkflowStep
{
    public ScriptStep()
    {
        Action = "Script";
    }
}

public class PluginStep : WorkflowStep
{
    public PluginStep()
    {
        Action = "Plugin";
    }
}

public class ServiceStep : WorkflowStep
{
    public ServiceStep()
    {
        Action = "Service";
    }
}
