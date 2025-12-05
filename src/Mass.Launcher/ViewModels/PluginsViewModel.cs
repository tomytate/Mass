using CommunityToolkit.Mvvm.Input;
using Mass.Core.Plugins;
using Mass.Core.UI;

namespace Mass.Launcher.ViewModels;

public partial class PluginsViewModel : ViewModelBase
{
    private readonly PluginLifecycleManager _lifecycleManager;

    public PluginsViewModel(PluginLifecycleManager lifecycleManager)
    {
        _lifecycleManager = lifecycleManager;
        Title = "Plugins";
        LoadPlugins();
    }

    public List<PluginInfo> Plugins { get; private set; } = new();

    private void LoadPlugins()
    {
        Plugins = _lifecycleManager.LoadedPlugins.Values
            .Select(p => new PluginInfo
            {
                Id = p.Manifest.Id,
                Name = p.Manifest.Name,
                Version = p.Manifest.Version,
                Author = p.Manifest.Author,
                Description = p.Manifest.Description,
                State = p.State.ToString(),
                IsEnabled = p.State == PluginState.Running,
                ErrorMessage = p.ErrorMessage
            })
            .ToList();
        
        OnPropertyChanged(nameof(Plugins));
    }

    [RelayCommand]
    private async Task TogglePluginAsync(string pluginId)
    {
        if (_lifecycleManager.LoadedPlugins.TryGetValue(pluginId, out var plugin))
        {
            if (plugin.State == PluginState.Running)
            {
                await _lifecycleManager.StopPluginAsync(pluginId);
            }
            else if (plugin.State == PluginState.Loaded || plugin.State == PluginState.Stopped)
            {
                await _lifecycleManager.StartPluginAsync(pluginId);
            }
        }
        
        LoadPlugins();
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadPlugins();
    }
}

public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? ErrorMessage { get; set; }
}
