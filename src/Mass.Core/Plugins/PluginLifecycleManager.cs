using System.Text.Json;
using Mass.Core.Abstractions;
using Mass.Spec.Contracts.Plugins;
using Microsoft.Extensions.Logging;

namespace Mass.Core.Plugins;

public class PluginLifecycleManager
{
    private readonly IPluginLoader _loader;
    private readonly IServiceProvider _services;
    private readonly ILogger<PluginLifecycleManager> _logger;
    private readonly string _persistencePath;
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public IReadOnlyDictionary<string, LoadedPlugin> LoadedPlugins => _loadedPlugins;

    public PluginLifecycleManager(
        IPluginLoader loader, 
        IServiceProvider services,
        ILogger<PluginLifecycleManager> logger)
    {
        _loader = loader;
        _services = services;
        _logger = logger;
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _persistencePath = Path.Combine(appData, "MassSuite", "plugins.json");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await LoadStateAsync(cancellationToken);
    }

    public async Task<bool> LoadPluginAsync(DiscoveredPlugin discoveredPlugin)
    {
        if (_loadedPlugins.ContainsKey(discoveredPlugin.Manifest.Id))
        {
            _logger.LogWarning("Plugin {PluginId} is already loaded.", discoveredPlugin.Manifest.Id);
            return false;
        }

        try
        {
            var plugin = _loader.LoadPlugin(discoveredPlugin.PluginPath, discoveredPlugin.Manifest);
            
            if (plugin == null)
            {
                _logger.LogError("Failed to load plugin {PluginId}.", discoveredPlugin.Manifest.Id);
                return false;
            }

            var loadedPlugin = new LoadedPlugin
            {
                Manifest = discoveredPlugin.Manifest,
                Plugin = plugin,
                State = PluginState.Loaded,
                PluginPath = discoveredPlugin.PluginPath
            };

            _loadedPlugins[discoveredPlugin.Manifest.Id] = loadedPlugin;
            
            // Initialize the plugin immediately upon loading
            try 
            {
                plugin.Init(_services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing plugin {PluginId}.", discoveredPlugin.Manifest.Id);
                loadedPlugin.State = PluginState.Failed;
                loadedPlugin.ErrorMessage = ex.Message;
                return false;
            }

            await SaveStateAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading plugin {PluginId}.", discoveredPlugin.Manifest.Id);
            return false;
        }
    }

    public async Task<bool> StartPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin) || loadedPlugin.Plugin == null)
        {
            return false;
        }

        if (loadedPlugin.State == PluginState.Running) return true;

        try
        {
            loadedPlugin.State = PluginState.Starting;
            await loadedPlugin.Plugin.StartAsync(cancellationToken);
            loadedPlugin.State = PluginState.Running;
            loadedPlugin.ErrorMessage = null;
            
            await SaveStateAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting plugin {PluginId}.", pluginId);
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = ex.Message;
            await SaveStateAsync();
            return false;
        }
    }

    public async Task<bool> StopPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin) || loadedPlugin.Plugin == null)
        {
            return false;
        }

        if (loadedPlugin.State == PluginState.Stopped) return true;

        try
        {
            loadedPlugin.State = PluginState.Stopping;
            await loadedPlugin.Plugin.StopAsync(cancellationToken);
            loadedPlugin.State = PluginState.Stopped;
            
            await SaveStateAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping plugin {PluginId}.", pluginId);
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = ex.Message;
            await SaveStateAsync();
            return false;
        }
    }

    public async Task UnloadPluginAsync(string pluginId)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            if (loadedPlugin.State == PluginState.Running)
            {
                await StopPluginAsync(pluginId);
            }

            _loader.UnloadPlugin(pluginId);
            _loadedPlugins.Remove(pluginId);
            await SaveStateAsync();
        }
    }

    private async Task SaveStateAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_persistencePath);
            if (directory != null) Directory.CreateDirectory(directory);

            var state = _loadedPlugins.Values.Select(p => new PluginStateDto
            {
                Id = p.Manifest.Id,
                State = p.State,
                PluginPath = p.PluginPath,
                Manifest = p.Manifest
            }).ToList();

            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await File.WriteAllTextAsync(_persistencePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist plugin state.");
        }
    }

    private async Task LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_persistencePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(_persistencePath, cancellationToken);
            var savedStates = JsonSerializer.Deserialize<List<PluginStateDto>>(json, _jsonOptions);

            if (savedStates == null) return;

            foreach (var savedState in savedStates)
            {
                // We need to re-load the plugin assembly
                // Assuming the path is still valid
                if (Directory.Exists(savedState.PluginPath))
                {
                    var discovered = new DiscoveredPlugin 
                    { 
                        Manifest = savedState.Manifest, 
                        PluginPath = savedState.PluginPath 
                    };
                    
                    if (await LoadPluginAsync(discovered))
                    {
                        // Restore state if it was running
                        if (savedState.State == PluginState.Running)
                        {
                            await StartPluginAsync(savedState.Id, cancellationToken);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load persisted plugin state.");
        }
    }
}

public class LoadedPlugin
{
    public PluginManifest Manifest { get; set; } = null!;
    public IPlugin? Plugin { get; set; }
    public PluginState State { get; set; }
    public string? ErrorMessage { get; set; }
    public string PluginPath { get; set; } = string.Empty;
}

public enum PluginState
{
    Discovered,
    Loaded,
    Starting,
    Running,
    Stopping,
    Stopped,
    Failed
}

public class PluginStateDto
{
    public string Id { get; set; } = string.Empty;
    public PluginState State { get; set; }
    public string PluginPath { get; set; } = string.Empty;
    public PluginManifest Manifest { get; set; } = null!;
}
