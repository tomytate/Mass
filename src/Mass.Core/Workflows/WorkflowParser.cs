using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mass.Core.Workflows;

public class WorkflowParser
{
    private readonly IDeserializer _yamlDeserializer;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowParser()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .Build();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public WorkflowDefinition ParseFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Workflow file not found: {filePath}");

        var content = File.ReadAllText(filePath);
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
            var rawData = _yamlDeserializer.Deserialize<object>(yaml);
            
            // Debug logging for test failure diagnosis
            // Console.WriteLine($"Deserialized type: {rawData?.GetType().FullName}");

            if (rawData is Dictionary<object, object> dictObj)
            {
                var dict = dictObj.ToDictionary(k => k.Key.ToString() ?? string.Empty, v => v.Value);
                return ConvertToWorkflowDefinition(dict);
            }
            else if (rawData is Dictionary<string, object> dictStr)
            {
                return ConvertToWorkflowDefinition(dictStr);
            }
            
            // Fallback for other dictionary types
            if (rawData is System.Collections.IDictionary dictionary)
            {
                 var dict = new Dictionary<string, object>();
                 foreach (System.Collections.DictionaryEntry entry in dictionary)
                 {
                     dict[entry.Key.ToString() ?? string.Empty] = entry.Value;
                 }
                 return ConvertToWorkflowDefinition(dict);
            }

            throw new InvalidOperationException($"Unexpected YAML root type: {rawData?.GetType().FullName}");
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
                    workflow.Parameters[kvp.Key.ToString() ?? string.Empty] = kvp.Value;
                }
            }
            else if (parametersObj is Dictionary<string, object> parametersDictStr)
            {
                foreach (var kvp in parametersDictStr)
                {
                    workflow.Parameters[kvp.Key] = kvp.Value;
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

        WorkflowStep step = type.ToLowerInvariant() switch
        {
            "command" => new CommandStep(),
            "httprequest" or "http" => new HttpRequestStep(),
            "script" => new ScriptStep(),
            "plugin" => new PluginStep(),
            "service" => new ServiceStep(),
            "burn" => new BurnStep(),
            "patch" => new PatchStep(),
            "device" => new DeviceStep(),
            "pxe" => new PxeStep(),
            _ => new CommandStep()
        };

        step.Id = GetStringValue(stepData, "id", Guid.NewGuid().ToString());
        step.Name = GetStringValue(stepData, "name");
        step.Condition = GetStringValue(stepData, "condition");
        step.MaxRetries = GetIntValue(stepData, "maxRetries", 0);
        step.RetryDelayMs = GetIntValue(stepData, "retryDelayMs", 1000);
        step.RunAlways = GetBoolValue(stepData, "runAlways", false);

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
        return data.TryGetValue(key, out var value) ? value?.ToString() ?? defaultValue : defaultValue;
    }

    private int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
    {
        if (data.TryGetValue(key, out var value))
        {
            if (value is int intValue) return intValue;
            if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    private bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
    {
        if (data.TryGetValue(key, out var value))
        {
            if (value is bool boolValue) return boolValue;
            if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }
}
