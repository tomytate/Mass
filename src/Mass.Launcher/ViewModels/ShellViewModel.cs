using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Interfaces;
using Mass.Core.Abstractions;
using Mass.Spec.Config;
using Mass.Core.UI;
using Mass.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Mass.Launcher.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public ViewModelBase? CurrentView => (_navigationService as Services.NavigationService)?.CurrentViewModel;

    private readonly IConfigurationService _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDialogService _dialogService;
    private readonly Mass.Core.Services.IIpcService _ipcService;

    [ObservableProperty]
    private bool _isConsentDialogVisible;

    [ObservableProperty]
    private ConsentDialogViewModel? _consentDialog;

    public ShellViewModel(
        INavigationService navigationService, 
        IConfigurationService config, 
        IServiceProvider serviceProvider,
        IDialogService dialogService,
        Mass.Core.Services.IIpcService ipcService,
        PluginLifecycleManager pluginLifecycle) // Added pluginLifecycle to match signature if needed, or remove if not used
    {
        _navigationService = navigationService;
        _config = config;
        _serviceProvider = serviceProvider;
        _dialogService = dialogService;
        _ipcService = ipcService;
        
        if (_navigationService is Services.NavigationService navService)
        {
            navService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Services.NavigationService.CurrentViewModel))
                {
                    OnPropertyChanged(nameof(CurrentView));
                }
            };
        }

        CheckTelemetryConsent();
    }

    private void CheckTelemetryConsent()
    {
        try 
        {
            var settings = _config.Get<Mass.Spec.Config.AppSettings>("AppSettings", new Mass.Spec.Config.AppSettings());
            if (settings != null && !settings.Telemetry.ConsentDecisionMade)
            {
                ShowConsentDialog();
            }
        }
        catch (Exception ex)
        {
            // Log but don't crash shell for consent check
            System.Diagnostics.Debug.WriteLine($"Error checking consent: {ex.Message}");
        }
    }

    private void ShowConsentDialog()
    {
        var dialog = _serviceProvider.GetRequiredService<ConsentDialogViewModel>();
        dialog.OnRequestClose += () => IsConsentDialogVisible = false;
        ConsentDialog = dialog;
        IsConsentDialogVisible = true;
    }

    [RelayCommand]
    public async Task NavigateHome() => await SafeNavigate(() => _navigationService.NavigateTo<HomeViewModel>());

    [RelayCommand]
    public async Task NavigateProUSB() => await SafeNavigate(() => _navigationService.NavigateTo<ProUSB.UI.ViewModels.MainViewModel>());

    [RelayCommand]
    public async Task NavigateSettings() => await SafeNavigate(() => _navigationService.NavigateTo<SettingsViewModel>());

    [RelayCommand]
    public async Task NavigatePlugins() => await SafeNavigate(() => _navigationService.NavigateTo<PluginsViewModel>());

    [RelayCommand]
    public async Task NavigateWorkflows() => await SafeNavigate(() => _navigationService.NavigateTo<WorkflowsViewModel>());

    [RelayCommand]
    public async Task NavigateHealth() => await SafeNavigate(() => _navigationService.NavigateTo<HealthViewModel>());

    [RelayCommand]
    public async Task NavigateLogs() => await SafeNavigate(() => _navigationService.NavigateTo<LogsViewModel>());


    [RelayCommand]
    public async Task NavigateOperationsConsole() => await SafeNavigate(() => _navigationService.NavigateTo<OperationsConsoleViewModel>());

    [RelayCommand]
    public async Task NavigateProPXEServer() => await SafeNavigate(() => _navigationService.NavigateTo<HomeViewModel>());

    [RelayCommand]
    public async Task NavigateScripting() => await SafeNavigate(() => _navigationService.NavigateTo<ScriptingViewModel>());

    [RelayCommand]
    public async Task StartServer()
    {
        try
        {
            bool success = await _ipcService.StartServerAsync(_serviceProvider);
            if (success)
            {
                await _dialogService.ShowMessageDialogAsync("Server Started", "ProPXEServer has been started successfully.");
            }
            else
            {
                await _dialogService.ShowErrorDialogAsync("Server Error", "Failed to start ProPXEServer.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Server Error", $"Error starting server: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StopServer()
    {
        try
        {
            bool success = await _ipcService.StopServerAsync();
            if (success)
            {
                await _dialogService.ShowMessageDialogAsync("Server Stopped", "ProPXEServer has been stopped.");
            }
            else
            {
                await _dialogService.ShowErrorDialogAsync("Server Error", "Failed to stop ProPXEServer.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Server Error", $"Error stopping server: {ex.Message}");
        }
    }

    private async Task SafeNavigate(Action navigationAction)
    {
        try
        {
            navigationAction();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Navigation Error", $"Failed to navigate: {ex.Message}");
        }
    }
}

