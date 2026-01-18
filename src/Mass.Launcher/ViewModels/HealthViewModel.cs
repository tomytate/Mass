using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Abstractions;
using Mass.Core.Interfaces;
using Mass.Core.Services;
using Mass.Core.UI;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mass.Launcher.ViewModels;

public partial class HealthViewModel : ViewModelBase, IDisposable
{
    private readonly IStatusService _statusService;
    private readonly INavigationService _navigationService;
    private readonly Mass.Core.Plugins.PluginLifecycleManager _pluginManager;
    private readonly Mass.Spec.Config.AppSettings _appSettings;
    private const int MaxHistoryPoints = 60;

    public SystemStatus CurrentStatus { get; private set; } = new();
    public ObservableCollection<ModuleStatus> ModuleStatuses { get; } = new();
    public ObservableCollection<double> CpuHistory { get; } = new();
    public ObservableCollection<double> MemoryHistory { get; } = new();

    public HealthViewModel(
        IStatusService statusService, 
        INavigationService navigationService,
        Mass.Core.Plugins.PluginLifecycleManager pluginManager,
        Mass.Core.Interfaces.IConfigurationService configService)
    {
        _statusService = statusService;
        _navigationService = navigationService;
        _pluginManager = pluginManager;
        _appSettings = configService.Get<Mass.Spec.Config.AppSettings>("AppSettings", new Mass.Spec.Config.AppSettings());
        Title = "System Health";

        // Initialize history with zeros
        for (int i = 0; i < MaxHistoryPoints; i++)
        {
            CpuHistory.Add(0);
            MemoryHistory.Add(0);
        }

        _statusService.StatusUpdated += OnStatusUpdated;
        _statusService.StartMonitoring();

        // Initial load
        UpdateModuleStatuses();
    }

    private void OnStatusUpdated(object? sender, SystemStatus status)
    {
        CurrentStatus = status;
        OnPropertyChanged(nameof(CurrentStatus));

        // Update charts on UI thread
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            CpuHistory.Add(status.CpuUsagePercent);
            if (CpuHistory.Count > MaxHistoryPoints) CpuHistory.RemoveAt(0);

            // Convert bytes to GB for chart readability
            MemoryHistory.Add(status.MemoryUsageBytes / 1024.0 / 1024.0 / 1024.0); 
            if (MemoryHistory.Count > MaxHistoryPoints) MemoryHistory.RemoveAt(0);
        });
    }

    private void UpdateModuleStatuses()
    {
        ModuleStatuses.Clear();
        foreach (var status in _statusService.GetModuleStatuses())
        {
            ModuleStatuses.Add(status);
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        UpdateModuleStatuses();
    }

    public void Dispose()
    {
        _statusService.StopMonitoring();
        _statusService.StatusUpdated -= OnStatusUpdated;
    }
}
