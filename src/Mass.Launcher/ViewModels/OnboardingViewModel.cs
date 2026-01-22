using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Interfaces;
using Mass.Core.Abstractions;
using Mass.Spec.Config;
using Mass.Core.UI;
using Mass.Core.Services;

namespace Mass.Launcher.ViewModels;

public partial class OnboardingViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IConfigurationService _configurationService;

    [ObservableProperty]
    private int _currentPageIndex = 0;

    [ObservableProperty]
    private bool _canGoNext = true;

    [ObservableProperty]
    private bool _canGoBack = false;

    public OnboardingViewModel(
        INavigationService navigationService,
        IConfigurationService configurationService)
    {
        _navigationService = navigationService;
        _configurationService = configurationService;
    }

    [RelayCommand]
    public void NextPage()
    {
        if (CurrentPageIndex < 2)
        {
            CurrentPageIndex++;
            UpdateNavigationState();
        }
        else
        {
            CompleteOnboarding();
        }
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            UpdateNavigationState();
        }
    }

    [RelayCommand]
    public void SkipOnboarding()
    {
        CompleteOnboarding();
    }

    private void UpdateNavigationState()
    {
        CanGoBack = CurrentPageIndex > 0;
        CanGoNext = true; // Logic to validate steps can be added here
        NextButtonText = CurrentPageIndex < 2 ? "Next" : "Get Started";
    }

    [ObservableProperty]
    private string _nextButtonText = "Next";

    private async void CompleteOnboarding()
    {
        // Save that first run is done
        var settings = _configurationService.Get<GeneralSettings>("General");
        // We need to update just the specific key usually, or the whole object.
        // The service interface is Set<T>(key, value).
        // Let's assume we can update individual properties or the root.
        
        // Since Set takes a key path:
        _configurationService.Set("General.IsFirstRun", false);
        await _configurationService.SaveAsync();

        // Navigate to Home
        _navigationService.NavigateTo<HomeViewModel>();
    }
}
