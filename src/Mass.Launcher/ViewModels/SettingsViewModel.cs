using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Interfaces;
using Mass.Spec.Config;
using Mass.Core.Security;
using Mass.Core.Services;
using Mass.Core.UI;
using Mass.Core.Updates;
using System.Collections.ObjectModel;

namespace Mass.Launcher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _config;
    private readonly IElevationService _elevationService;
    private readonly IUpdateService _updateService;

    public bool IsElevated => _elevationService.IsElevated;
    public string ElevationStatus => IsElevated ? "Running as Administrator" : "Running as Standard User";
    public string ElevationIcon => IsElevated ? "🔓" : "🔒";
    public string CurrentVersion => _updateService.CurrentVersion;

    [ObservableProperty]
    private string _updateStatus = "Not checked";

    [ObservableProperty]
    private bool _checkingForUpdates;

    private readonly Mass.Core.Plugins.PluginLifecycleManager _pluginManager;
    private readonly ILocalizationService _localizationService;

    public ObservableCollection<Mass.Core.Plugins.LoadedPlugin> Plugins { get; } = new();
    
    public IEnumerable<System.Globalization.CultureInfo> AvailableLanguages => _localizationService.AvailableCultures;
    
    public System.Globalization.CultureInfo SelectedLanguage
    {
        get => _localizationService.CurrentCulture;
        set
        {
            if (value != null)
            {
                _localizationService.SetLanguage(value.Name);
                OnPropertyChanged();
            }
        }
    }

    public SettingsViewModel(
        IConfigurationService config, 
        IElevationService elevationService, 
        IUpdateService updateService,
        Mass.Core.Plugins.PluginLifecycleManager pluginManager,
        ILocalizationService localizationService)
    {
        _config = config;
        _elevationService = elevationService;
        _updateService = updateService;
        _pluginManager = pluginManager;
        _localizationService = localizationService;
        Title = "Settings";
        LoadSettings();
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        Plugins.Clear();
        foreach (var plugin in _pluginManager.LoadedPlugins.Values)
        {
            Plugins.Add(plugin);
        }
    }

    [RelayCommand]
    private void TogglePlugin(Mass.Core.Plugins.LoadedPlugin plugin)
    {
        // Logic to enable/disable plugin
        // For now just update the manifest in memory, persistence would be needed
        plugin.Manifest.Enabled = !plugin.Manifest.Enabled;
        // Trigger reload or restart requirement notification
    }


    private AppSettings _appSettings = new();
    public AppSettings AppSettings
    {
        get => _appSettings;
        set
        {
            _appSettings = value;
            OnPropertyChanged();
        }
    }

    private void LoadSettings()
    {
        AppSettings = _config.Get("AppSettings", new AppSettings());
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _config.Set("AppSettings", AppSettings);
        await _config.SaveAsync();
    }

    [RelayCommand]
    private void Reset()
    {
        AppSettings = new AppSettings();
        _config.Set("AppSettings", AppSettings);
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        AppSettings.General.Theme = theme;
        _config.Set("AppSettings", AppSettings);
        OnPropertyChanged(nameof(AppSettings));
    }

    [RelayCommand]
    private void RestartAsAdmin()
    {
        _elevationService.RestartAsAdmin();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        CheckingForUpdates = true;
        UpdateStatus = "Checking for updates...";

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();
            UpdateStatus = result.Message;

            if (result.UpdateAvailable && result.LatestVersion != null)
            {
                UpdateStatus = $"Version {result.LatestVersion.Version} available!";
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Error: {ex.Message}";
        }
        finally
        {
            CheckingForUpdates = false;
        }
    }
    [RelayCommand]
    private void OpenTelemetryLogs()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "logs", "telemetry");
        if (Directory.Exists(logPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = logPath,
                UseShellExecute = true
            });
        }
    }
}
