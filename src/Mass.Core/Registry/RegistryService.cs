using System.Collections.Concurrent;
using System.Text.Json;

namespace Mass.Core.Registry;

/// <summary>
/// Thread-safe central registry for steps, plugins, and handlers.
/// </summary>
public class RegistryService
{
    private readonly ConcurrentDictionary<string, StepDescriptor> _steps = new();
    private readonly ConcurrentDictionary<string, LoadedPluginDescriptor> _plugins = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly string _registryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the RegistryService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for DI resolution.</param>
    public RegistryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var massSuiteDir = Path.Combine(appData, "MassSuite");
        Directory.CreateDirectory(massSuiteDir);
        _registryPath = Path.Combine(massSuiteDir, "registry.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Registers a workflow step.
    /// </summary>
    /// <param name="descriptor">The step descriptor.</param>
    public void RegisterStep(StepDescriptor descriptor)
    {
        if (string.IsNullOrEmpty(descriptor.Id))
            throw new ArgumentException("Step descriptor must have an Id", nameof(descriptor));

        _steps[descriptor.Id] = descriptor;
    }

    /// <summary>
    /// Lists all registered steps.
    /// </summary>
    /// <returns>Read-only list of step descriptors.</returns>
    public IReadOnlyList<StepDescriptor> ListSteps()
    {
        return _steps.Values.ToList();
    }

    /// <summary>
    /// Finds a step by its identifier.
    /// </summary>
    /// <param name="id">The step identifier.</param>
    /// <returns>The step descriptor if found; otherwise, null.</returns>
    public StepDescriptor? FindStep(string id)
    {
        return _steps.TryGetValue(id, out var descriptor) ? descriptor : null;
    }

    /// <summary>
    /// Registers a plugin.
    /// </summary>
    /// <param name="descriptor">The plugin descriptor.</param>
    public void RegisterPlugin(LoadedPluginDescriptor descriptor)
    {
        if (string.IsNullOrEmpty(descriptor.Id))
            throw new ArgumentException("Plugin descriptor must have an Id", nameof(descriptor));

        _plugins[descriptor.Id] = descriptor;
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IReadOnlyDictionary<string, LoadedPluginDescriptor> LoadedPlugins => _plugins;

    /// <summary>
    /// Resolves a handler instance from the service provider.
    /// </summary>
    /// <param name="handlerType">The type of handler to resolve.</param>
    /// <returns>The handler instance, or null if not found.</returns>
    public object? ResolveHandler(Type handlerType)
    {
        return _serviceProvider.GetService(handlerType);
    }

    /// <summary>
    /// Saves the registry to persistent storage using atomic writes.
    /// </summary>
    public void Save()
    {
        var data = new RegistryData
        {
            Steps = _steps.Values.ToList(),
            Plugins = _plugins.Values.ToList()
        };

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var tempPath = _registryPath + ".tmp";

        // Write to temp file first
        File.WriteAllText(tempPath, json);

        // Atomic rename (on NTFS)
        File.Move(tempPath, _registryPath, overwrite: true);
    }

    /// <summary>
    /// Loads the registry from persistent storage.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_registryPath))
            return;

        try
        {
            var json = File.ReadAllText(_registryPath);
            var data = JsonSerializer.Deserialize<RegistryData>(json, _jsonOptions);

            if (data == null)
                return;

            _steps.Clear();
            _plugins.Clear();

            foreach (var step in data.Steps)
            {
                _steps[step.Id] = step;
            }

            foreach (var plugin in data.Plugins)
            {
                _plugins[plugin.Id] = plugin;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load registry from {_registryPath}", ex);
        }
    }

    /// <summary>
    /// Internal data structure for JSON serialization.
    /// </summary>
    private class RegistryData
    {
        public List<StepDescriptor> Steps { get; set; } = new();
        public List<LoadedPluginDescriptor> Plugins { get; set; } = new();
    }
}
