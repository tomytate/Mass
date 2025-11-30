using System.Diagnostics;

namespace Mass.Core.Orchestration;

public class WorkflowExecutor
{
    public async Task<WorkflowResult> ExecuteAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var context = new WorkflowContext
        {
            CancellationToken = cancellationToken
        };

        foreach (var param in workflow.Parameters)
        {
            context.SetVariable(param.Key, param.Value);
        }

        context.Log($"Starting workflow: {workflow.Name}");

        try
        {
            foreach (var step in workflow.Steps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    context.Log("Workflow cancelled by user");
                    return new WorkflowResult { Success = false, Message = "Cancelled", Context = context };
                }

                if (!string.IsNullOrEmpty(step.Condition) && !EvaluateCondition(step.Condition, context))
                {
                    context.Log($"Skipping step '{step.Name}' - condition not met");
                    continue;
                }

                context.Log($"Executing step: {step.Name} ({step.Type})");

                var success = await ExecuteStepAsync(step, context);

                if (!success && !step.RunAlways)
                {
                    context.Log($"Step '{step.Name}' failed");
                    return new WorkflowResult { Success = false, Message = $"Step '{step.Name}' failed", Context = context };
                }
            }

            context.Log($"Workflow completed successfully: {workflow.Name}");
            return new WorkflowResult { Success = true, Message = "Completed", Context = context };
        }
        catch (Exception ex)
        {
            context.Log($"Workflow error: {ex.Message}");
            return new WorkflowResult { Success = false, Message = ex.Message, Context = context };
        }
    }

    private async Task<bool> ExecuteStepAsync(WorkflowStep step, WorkflowContext context)
    {
        var retries = 0;
        Exception? lastException = null;

        while (retries <= step.MaxRetries)
        {
            try
            {
                var result = step switch
                {
                    CommandStep commandStep => await ExecuteCommandStepAsync(commandStep, context),
                    HttpRequestStep httpStep => await ExecuteHttpRequestStepAsync(httpStep, context),
                    ScriptStep scriptStep => await ExecuteScriptStepAsync(scriptStep, context),
                    _ => throw new NotSupportedException($"Step type '{step.Type}' is not supported")
                };

                context.SetStepResult(step.Id, result);
                return true;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retries++;

                if (retries <= step.MaxRetries)
                {
                    context.Log($"Step '{step.Name}' failed (attempt {retries}/{step.MaxRetries + 1}): {ex.Message}");
                    await Task.Delay(step.RetryDelayMs, context.CancellationToken);
                }
            }
        }

        context.Log($"Step '{step.Name}' failed after {retries} attempts: {lastException?.Message}");
        return false;
    }

    private async Task<object> ExecuteCommandStepAsync(CommandStep step, WorkflowContext context)
    {
        var command = context.InterpolateString(step.Parameters.GetValueOrDefault("command")?.ToString() ?? string.Empty);
        var workingDir = context.InterpolateString(step.Parameters.GetValueOrDefault("workingDirectory")?.ToString() ?? Environment.CurrentDirectory);

        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(context.CancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {error}");
        }

        context.Log($"Command output: {output}");
        return output;
    }

    private async Task<object> ExecuteHttpRequestStepAsync(HttpRequestStep step, WorkflowContext context)
    {
        var url = context.InterpolateString(step.Parameters.GetValueOrDefault("url")?.ToString() ?? string.Empty);
        var method = step.Parameters.GetValueOrDefault("method")?.ToString() ?? "GET";

        using var client = new HttpClient();
        var response = method.ToUpper() switch
        {
            "GET" => await client.GetAsync(url, context.CancellationToken),
            "POST" => await client.PostAsync(url, null, context.CancellationToken),
            _ => throw new NotSupportedException($"HTTP method '{method}' is not supported")
        };

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(context.CancellationToken);

        context.Log($"HTTP {method} {url} - Status: {response.StatusCode}");
        return content;
    }

    private async Task<object> ExecuteScriptStepAsync(ScriptStep step, WorkflowContext context)
    {
        var scriptPath = context.InterpolateString(step.Parameters.GetValueOrDefault("path")?.ToString() ?? string.Empty);
        var arguments = context.InterpolateString(step.Parameters.GetValueOrDefault("arguments")?.ToString() ?? string.Empty);
        var workingDir = context.InterpolateString(step.Parameters.GetValueOrDefault("workingDirectory")?.ToString() ?? Environment.CurrentDirectory);

        if (!File.Exists(scriptPath))
        {
            // Try to find relative to working directory
            var relativePath = Path.Combine(workingDir, scriptPath);
            if (File.Exists(relativePath))
            {
                scriptPath = relativePath;
            }
            else
            {
                throw new FileNotFoundException($"Script file not found: {scriptPath}");
            }
        }

        var extension = Path.GetExtension(scriptPath).ToLowerInvariant();
        var processInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        switch (extension)
        {
            case ".bat":
            case ".cmd":
                processInfo.FileName = "cmd.exe";
                processInfo.Arguments = $"/c \"{scriptPath}\" {arguments}";
                break;
            case ".ps1":
                processInfo.FileName = "powershell.exe";
                processInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}";
                break;
            default:
                // Try to execute directly (e.g. .exe or associated file)
                processInfo.FileName = scriptPath;
                processInfo.Arguments = arguments;
                break;
        }

        using var process = Process.Start(processInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start script process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(context.CancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Script failed with exit code {process.ExitCode}: {error}");
        }

        context.Log($"Script output: {output}");
        return output;
    }

    private bool EvaluateCondition(string condition, WorkflowContext context)
    {
        var interpolated = context.InterpolateString(condition);
        return interpolated.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}

public class WorkflowResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public WorkflowContext? Context { get; set; }
}
