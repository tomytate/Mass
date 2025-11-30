using Mass.Core.Abstractions;

namespace Mass.Core.Plugins;

public class PluginLifecycleManager
{
    private readonly PluginLoader _loader;
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();

    public IReadOnlyDictionary<string, LoadedPlugin> LoadedPlugins => _loadedPlugins;

    public PluginLifecycleManager(PluginLoader loader)
    {
        _loader = loader;
    }

    public async Task<bool> LoadPluginAsync(DiscoveredPlugin discoveredPlugin, IServiceProvider services)
    {
        if (_loadedPlugins.ContainsKey(discoveredPlugin.Manifest.Id))
        {
            return false;
        }

        try
        {
            // Note: PluginLoader needs to be updated to return IModule instead of IPlugin
            // For now assuming we cast or update PluginLoader
            var module = _loader.LoadPlugin(discoveredPlugin.PluginPath, discoveredPlugin.Manifest) as IModule;
            
            if (module == null)
            {
                return false;
            }

            var loadedPlugin = new LoadedPlugin
            {
                Manifest = discoveredPlugin.Manifest,
                Module = module,
                State = PluginState.Loaded
            };

            _loadedPlugins[discoveredPlugin.Manifest.Id] = loadedPlugin;

            return true;
        }
        catch (Exception ex)
        {
            var errorPlugin = new LoadedPlugin
            {
                Manifest = discoveredPlugin.Manifest,
                Module = null,
                State = PluginState.Failed,
                ErrorMessage = ex.Message
            };
            _loadedPlugins[discoveredPlugin.Manifest.Id] = errorPlugin;
            return false;
        }
    }

    public async Task<bool> InitializePluginAsync(string pluginId, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin) || loadedPlugin.Module == null)
        {
            return false;
        }

        try
        {
            loadedPlugin.State = PluginState.Starting;
            await loadedPlugin.Module.InitializeAsync(services, cancellationToken);
            await loadedPlugin.Module.ActivateAsync(cancellationToken);
            loadedPlugin.State = PluginState.Running;
            return true;
        }
        catch (Exception ex)
        {
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task<bool> ShutdownPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin) || loadedPlugin.Module == null)
        {
            return false;
        }

        try
        {
            loadedPlugin.State = PluginState.Stopping;
            await loadedPlugin.Module.DeactivateAsync(cancellationToken);
            await loadedPlugin.Module.UnloadAsync(cancellationToken);
            loadedPlugin.State = PluginState.Stopped;
            return true;
        }
        catch (Exception ex)
        {
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = ex.Message;
            return false;
        }
    }

    public void UnloadPlugin(string pluginId)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            _loader.UnloadPlugin(pluginId);
            _loadedPlugins.Remove(pluginId);
        }
    }
}

public class LoadedPlugin
{
    public PluginManifest Manifest { get; set; } = null!;
    public IModule? Module { get; set; }
    public PluginState State { get; set; }
    public string? ErrorMessage { get; set; }
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
