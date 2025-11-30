using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Abstractions;
using Mass.Core.Configuration;
using Mass.Core.UI;
using Microsoft.Extensions.DependencyInjection;
using ProUSB.UI.ViewModels;

namespace Mass.Launcher.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public ViewModelBase? CurrentView => (_navigationService as Services.NavigationService)?.CurrentViewModel;

    private readonly IConfigurationService _config;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isConsentDialogVisible;

    [ObservableProperty]
    private ConsentDialogViewModel? _consentDialog;

    public ShellViewModel(INavigationService navigationService, IConfigurationService config, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _config = config;
        _serviceProvider = serviceProvider;
        
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
        var settings = _config.Get<Mass.Core.Configuration.AppSettings>("AppSettings", new Mass.Core.Configuration.AppSettings());
        if (settings != null && !settings.Telemetry.ConsentDecisionMade)
        {
            ShowConsentDialog();
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
    public void NavigateHome() => _navigationService.NavigateTo<HomeViewModel>();

    [RelayCommand]
    public void NavigateProUsb() => _navigationService.NavigateTo<MainViewModel>();

    [RelayCommand]
    public void NavigateSettings() => _navigationService.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    public void NavigatePlugins() => _navigationService.NavigateTo<PluginsViewModel>();

    [RelayCommand]
    public void NavigateWorkflows() => _navigationService.NavigateTo<WorkflowsViewModel>();

    [RelayCommand]
    public void NavigateHealth() => _navigationService.NavigateTo<HealthViewModel>();

    [RelayCommand]
    public void NavigateLogs() => _navigationService.NavigateTo<LogsViewModel>();


    [RelayCommand]
    public void NavigateOperationsConsole() => _navigationService.NavigateTo<OperationsConsoleViewModel>();

    [RelayCommand]
    public void NavigateMassBoot() => _navigationService.NavigateTo<HomeViewModel>();

    [RelayCommand]
    public void NavigateScripting() => _navigationService.NavigateTo<ScriptingViewModel>();
}

