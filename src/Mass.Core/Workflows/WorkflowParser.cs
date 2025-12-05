using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Mass.Spec.Contracts.Workflow;

namespace Mass.Core.Workflows;

public class WorkflowParser
{
    private readonly IDeserializer _yamlDeserializer;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowParser()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<WorkflowDefinition> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Workflow file not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".yaml" or ".yml" => ParseYaml(content),
            ".json" => ParseJson(content),
            _ => throw new NotSupportedException($"File extension '{extension}' is not supported. Use .yaml, .yml, or .json")
        };
    }

    public WorkflowDefinition ParseYaml(string yaml)
    {
        try
        {
            // YamlDotNet can deserialize directly to the object graph if it matches
            // However, Mass.Spec types might need custom mapping if the YAML structure is loose
            // For now, we'll try direct deserialization first, then fallback to dictionary mapping if needed
            
            // Note: Mass.Spec types use standard properties. 
            // We need to ensure polymorphic step deserialization works.
            // Since YamlDotNet doesn't support polymorphic deserialization easily without tags,
            // we might need to parse to a dictionary first and then map manually as before.
            
            var rawData = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            return ConvertToWorkflowDefinition(rawData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse YAML: {ex.Message}", ex);
        }
    }

    public WorkflowDefinition ParseJson(string json)
    {
        try
        {
            var rawData = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
            if (rawData == null)
                throw new InvalidOperationException("Failed to deserialize JSON");

            return ConvertToWorkflowDefinition(rawData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON: {ex.Message}", ex);
        }
    }

    private WorkflowDefinition ConvertToWorkflowDefinition(Dictionary<string, object> data)
    {
        var workflow = new WorkflowDefinition
        {
            Id = GetStringValue(data, "id"),
            Name = GetStringValue(data, "name"),
            Description = GetStringValue(data, "description"),
            Version = GetStringValue(data, "version", "1.0.0")
        };

        if (data.TryGetValue("parameters", out var parametersObj))
        {
            if (parametersObj is Dictionary<object, object> parametersDictObj)
            {
                foreach (var kvp in parametersDictObj)
                {
                    workflow.ParameterValues[kvp.Key.ToString() ?? string.Empty] = kvp.Value;
                }
            }
            else if (parametersObj is Dictionary<string, object> parametersDictStr)
            {
                foreach (var kvp in parametersDictStr)
                {
                    workflow.ParameterValues[kvp.Key] = kvp.Value;
                }
            }
        }

        if (data.TryGetValue("steps", out var stepsObj) && stepsObj is List<object> stepsList)
        {
            foreach (var stepObj in stepsList)
            {
                Dictionary<string, object>? stepData = null;

                if (stepObj is Dictionary<object, object> stepDictObj)
                {
                    stepData = stepDictObj.ToDictionary(k => k.Key.ToString() ?? string.Empty, v => v.Value);
                }
                else if (stepObj is Dictionary<string, object> stepDictStr)
                {
                    stepData = stepDictStr;
                }

                if (stepData != null)
                {
                    var step = ConvertToWorkflowStep(stepData);
                    workflow.Steps.Add(step);
                }
            }
        }

        return workflow;
    }

    private WorkflowStep ConvertToWorkflowStep(Dictionary<string, object> stepData)
    {
        var type = GetStringValue(stepData, "type", "Command");

        // Map to specific Mass.Spec step types
        // Note: Mass.Spec might define these as subclasses or a single class with an Action property
        // Let's assume Mass.Spec uses a single WorkflowStep class with an 'Action' property for simplicity
        // based on previous file views, or if it has subclasses, we instantiate them.
        // Looking at previous view of Mass.Spec/Contracts/Workflow/WorkflowStep.cs (Step 836), it was 1384 bytes.
        // It likely has properties. If it's a base class, we need to know the subclasses.
        // For now, I will assume a generic WorkflowStep and set the Action property.
        
        var step = new WorkflowStep
        {
            Id = GetStringValue(stepData, "id", Guid.NewGuid().ToString()),
            Name = GetStringValue(stepData, "name"),
            Action = type, // Mapping 'type' to 'Action'
            Condition = GetStringValue(stepData, "condition"),
            MaxRetries = GetIntValue(stepData, "maxRetries", 0),
            RetryDelayMs = GetIntValue(stepData, "retryDelayMs", 1000),
            RunAlways = GetBoolValue(stepData, "runAlways", false)
        };

        if (stepData.TryGetValue("parameters", out var parametersObj))
        {
            if (parametersObj is Dictionary<object, object> parametersDictObj)
            {
                foreach (var kvp in parametersDictObj)
                {
                    step.Parameters[kvp.Key.ToString() ?? string.Empty] = kvp.Value;
                }
            }
            else if (parametersObj is Dictionary<string, object> parametersDictStr)
            {
                foreach (var kvp in parametersDictStr)
                {
                    step.Parameters[kvp.Key] = kvp.Value;
                }
            }
        }

        return step;
    }

    private string GetStringValue(Dictionary<string, object> data, string key, string defaultValue = "")
    {
        // Case-insensitive lookup
        var entry = data.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return !entry.Equals(default(KeyValuePair<string, object>)) ? entry.Value?.ToString() ?? defaultValue : defaultValue;
    }

    private int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
    {
        var entry = data.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (!entry.Equals(default(KeyValuePair<string, object>)))
        {
            if (entry.Value is int intValue) return intValue;
            if (int.TryParse(entry.Value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    private bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
    {
        var entry = data.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (!entry.Equals(default(KeyValuePair<string, object>)))
        {
            if (entry.Value is bool boolValue) return boolValue;
            if (bool.TryParse(entry.Value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }
}
