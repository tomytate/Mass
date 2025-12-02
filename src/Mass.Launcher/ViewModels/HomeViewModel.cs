using CommunityToolkit.Mvvm.Input;
using Mass.Core.Abstractions;
using Mass.Core.Services;
using Mass.Core.UI;
using Mass.Launcher.Models;
using System.Collections.ObjectModel;

namespace Mass.Launcher.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IStatusService _statusService;
    private readonly IActivityService _activityService;

    public List<QuickActionCard> QuickActions { get; }
    public ObservableCollection<ModuleStatusCard> ModuleStatus { get; } = new();
    public ObservableCollection<ActivityItem> RecentActivity { get; } = new();
    public ObservableCollection<FavoriteItem> Favorites { get; } = new();
    public SystemStatus SystemStatus { get; private set; } = new();
    public string WelcomeMessage { get; }
    public string VersionInfo { get; }

    private readonly IDialogService _dialogService;

    public HomeViewModel(
        INavigationService navigationService,
        IStatusService statusService,
        IActivityService activityService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _statusService = statusService;
        _activityService = activityService;
        _dialogService = dialogService;
        
        Title = "Home";
        // ... (rest of constructor)
        
        WelcomeMessage = $"Welcome to Mass Suite";
        VersionInfo = "v1.0.0 - .NET 10.0";
        QuickActions = new List<QuickActionCard>
        {
            new()
            {
                Icon = "💾",
                Title = "ProUSB",
                Description = "Create bootable USB drives",
                NavigationTarget = "ProUSB"
            },
            new()
            {
                Icon = "🖥️",
                Title = "MassBoot",
                Description = "Network PXE boot server",
                NavigationTarget = "MassBoot"
            },
            new()
            {
                Icon = "⚙️",
                Title = "Workflows",
                Description = "Automate operations",
                NavigationTarget = "Workflows"
            },
            new()
            {
                Icon = "🔌",
                Title = "Plugins",
                Description = "Manage extensions",
                NavigationTarget = "Plugins"
            }
        };

        LoadData();
    }

    private void LoadData()
    {
        // Load Module Status
        ModuleStatus.Clear();
        foreach (var status in _statusService.GetModuleStatuses())
        {
            ModuleStatus.Add(new ModuleStatusCard
            {
                Name = status.Name,
                Status = status.Status,
                Icon = status.Icon,
                StatusColor = status.Color
            });
        }

        // Load System Status
        SystemStatus = _statusService.GetSystemStatus();
        OnPropertyChanged(nameof(SystemStatus));

        // Load Recent Activity
        RecentActivity.Clear();
        foreach (var activity in _activityService.GetRecentActivities())
        {
            RecentActivity.Add(activity);
        }

        // Load Favorites
        Favorites.Clear();
        foreach (var fav in _activityService.GetFavorites())
        {
            Favorites.Add(fav);
        }
    }

    [RelayCommand]
    private async Task NavigateToModule(string target)
    {
        try
        {
            switch (target)
            {
                case "ProUSB":
                    _navigationService.NavigateTo<ProUSB.UI.ViewModels.MainViewModel>();
                    break;
                case "MassBoot":
                    // TODO: Navigate to MassBoot when ready
                    break;
                case "Workflows":
                    _navigationService.NavigateTo<WorkflowsViewModel>();
                    break;
                case "Plugins":
                    _navigationService.NavigateTo<PluginsViewModel>();
                    break;
                case "Settings":
                    _navigationService.NavigateTo<SettingsViewModel>();
                    break;
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Navigation Error", $"Failed to navigate to {target}: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task NavigateToFavorite(FavoriteItem favorite)
    {
        await NavigateToModule(favorite.Target);
    }
}

public class ModuleStatusCard
{
    public string Icon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
}
