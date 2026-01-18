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
    private readonly System.Net.Http.HttpClient _httpClient = new();

    public ViewModelBase? CurrentView => (_navigationService as Services.NavigationService)?.CurrentViewModel;

    private readonly IConfigurationService _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDialogService _dialogService;
    private readonly Mass.Core.Services.IIpcService _ipcService;

    [ObservableProperty]
    private bool _isConsentDialogVisible;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [RelayCommand]
    public void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    [ObservableProperty]
    private ConsentDialogViewModel? _consentDialog;

    public ToastViewModel Toast { get; }

    public ShellViewModel(
        INavigationService navigationService,
        IConfigurationService config,
        IServiceProvider serviceProvider,
        IDialogService dialogService,
        Mass.Core.Services.IIpcService ipcService,
        PluginLifecycleManager pluginLifecycle,
        ToastViewModel toastViewModel) 
    {
        _navigationService = navigationService;
        _config = config;
        _serviceProvider = serviceProvider;
        _dialogService = dialogService;
        _ipcService = ipcService;
        Toast = toastViewModel;

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

    }

    [RelayCommand]
    public async Task NavigateHome() => await SafeNavigate(() => _navigationService.NavigateTo<HomeViewModel>());

    [RelayCommand]
    public async Task NavigateProUsb() => await SafeNavigate(() => _navigationService.NavigateTo<ProUSB.UI.ViewModels.MainViewModel>());

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
    public async Task NavigateMassBoot()
    {
        try
        {
            var url = "http://localhost:5054";
            
            // Pre-flight check
            try 
            {
                await _dialogService.ShowMessageDialogAsync("MassBoot", "Checking MassBoot availability...");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _httpClient.GetAsync(url, cts.Token);
            }
            catch
            {
                // Verify specific error or just warn user
                await _dialogService.ShowErrorDialogAsync("Connection Warning", "MassBoot UI might not be ready (Port 5054). Opening anyway...");
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Navigation Error", $"Failed to open MassBoot Web UI: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task NavigateScripting() => await SafeNavigate(() => _navigationService.NavigateTo<ScriptingViewModel>());

    [RelayCommand]
    public async Task NavigateOperationsConsole() => await SafeNavigate(() => _navigationService.NavigateTo<OperationsConsoleViewModel>());

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

