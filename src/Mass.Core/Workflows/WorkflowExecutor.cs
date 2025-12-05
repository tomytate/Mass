using System.Diagnostics;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Spec.Contracts.Logging;
using Mass.Spec.Contracts.Workflow;

namespace Mass.Core.Workflows;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly ILogService _logger;

    public WorkflowExecutor(ILogService logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateAsync(WorkflowDefinition workflow, CancellationToken ct = default)
    {
        var validator = new WorkflowValidator();
        return await Task.FromResult(validator.Validate(workflow));
    }

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowDefinition workflow, 
        WorkflowExecutionOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new WorkflowExecutionOptions();
        
        var context = new WorkflowContext
        {
            CancellationToken = ct
        };

        // Initialize parameters
        foreach (var param in workflow.ParameterValues)
        {
            context.SetVariable(param.Key, param.Value);
        }

        _logger.LogInformation($"Starting workflow: {workflow.Name}", "Workflow");

        try
        {
            foreach (var step in workflow.Steps)
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Workflow cancelled by user", "Workflow");
                    return new WorkflowResult 
                    { 
                        Success = false, 
                        CompletedSteps = context.ExecutionHistory,
                        Error = Mass.Spec.Exceptions.MassErrorCodes.Cancelled
                    };
                }

                if (!string.IsNullOrEmpty(step.Condition) && !EvaluateCondition(step.Condition, context))
                {
                    _logger.LogInformation($"Skipping step '{step.Name}' - condition not met", "Workflow");
                    continue;
                }

                _logger.LogInformation($"Executing step: {step.Name} ({step.Action})", "Workflow");

                var success = await ExecuteStepAsync(step, context);

                if (!success && !step.RunAlways)
                {
                    _logger.LogError($"Step '{step.Name}' failed", null, "Workflow");
                    return new WorkflowResult 
                    { 
                        Success = false, 
                        CompletedSteps = context.ExecutionHistory,
                        Error = new Mass.Spec.Exceptions.ErrorCode("step_failed", $"Step '{step.Name}' failed", false)
                    };
                }
            }

            _logger.LogInformation($"Workflow completed successfully: {workflow.Name}", "Workflow");
            return new WorkflowResult 
            { 
                Success = true, 
                CompletedSteps = context.ExecutionHistory
            };
        }
        catch (Mass.Spec.Exceptions.OperationException opEx)
        {
            _logger.LogError($"Workflow failed: {opEx.Message}", opEx, "Workflow");
            return new WorkflowResult 
            { 
                Success = false, 
                CompletedSteps = context.ExecutionHistory,
                Error = opEx.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Workflow error", ex, "Workflow");
            return new WorkflowResult 
            { 
                Success = false, 
                CompletedSteps = context.ExecutionHistory,
                Error = new Mass.Spec.Exceptions.ErrorCode("general_error", ex.Message, false)
            };
        }
    }

    private async Task<bool> ExecuteStepAsync(WorkflowStep step, WorkflowContext context)
    {
        var retries = 0;
        Exception? lastException = null;
        object? result = null;
        bool success = false;

        while (retries <= step.MaxRetries)
        {
            try
            {
                // In a real implementation, we would use RegistryService to resolve handlers
                // For now, we keep the hardcoded switch for basic types, but map them to generic execution
                
                result = step.Action.ToLowerInvariant() switch
                {
                    "command" => await ExecuteCommandStepAsync(step, context),
                    "httprequest" or "http" => await ExecuteHttpRequestStepAsync(step, context),
                    "script" => await ExecuteScriptStepAsync(step, context),
                    _ => await Task.FromResult<object>("Unknown step type - simulated success")
                };

                context.SetStepResult(step.Id, result!);
                success = true;
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retries++;

                if (retries <= step.MaxRetries)
                {
                    _logger.LogWarning($"Step '{step.Name}' failed (attempt {retries}/{step.MaxRetries + 1})", "Workflow");
                    await Task.Delay(step.RetryDelayMs, context.CancellationToken);
                }
            }
        }

        var stepResult = new Mass.Spec.Contracts.Workflow.WorkflowStepResult
        {
            StepId = step.Id,
            Success = success,
            Output = result,
            Error = success ? null : lastException?.Message
        };
        context.ExecutionHistory.Add(stepResult);

        if (!success)
        {
            _logger.LogError($"Step '{step.Name}' failed after {retries} attempts", lastException, "Workflow");
        }

        return success;
    }

    private async Task<object> ExecuteCommandStepAsync(WorkflowStep step, WorkflowContext context)
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

        return output;
    }

    private async Task<object> ExecuteHttpRequestStepAsync(WorkflowStep step, WorkflowContext context)
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

        return content;
    }

    private async Task<object> ExecuteScriptStepAsync(WorkflowStep step, WorkflowContext context)
    {
        var scriptPath = context.InterpolateString(step.Parameters.GetValueOrDefault("path")?.ToString() ?? string.Empty);
        var arguments = context.InterpolateString(step.Parameters.GetValueOrDefault("arguments")?.ToString() ?? string.Empty);
        var workingDir = context.InterpolateString(step.Parameters.GetValueOrDefault("workingDirectory")?.ToString() ?? Environment.CurrentDirectory);

        if (!File.Exists(scriptPath))
        {
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

        return output;
    }

    private bool EvaluateCondition(string condition, WorkflowContext context)
    {
        var interpolated = context.InterpolateString(condition);
        return interpolated.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
