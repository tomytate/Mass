using CommunityToolkit.Mvvm.ComponentModel;
using Mass.Core.Abstractions;
using Mass.Core.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Mass.Launcher.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public bool CanGoBack => false;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        if (CurrentViewModel is INavigable navigableFrom)
        {
            navigableFrom.OnNavigatedFrom();
        }

        try
        {
            var viewModel = _serviceProvider.GetRequiredService(viewModelType) as ViewModelBase;
            if (viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Navigation] FATAL: ViewModel resolved as null for {viewModelType.Name}");
                throw new InvalidOperationException($"Could not resolve ViewModel of type {viewModelType.Name}");
            }

            CurrentViewModel = viewModel;
            System.Diagnostics.Debug.WriteLine($"[Navigation] Success: Navigated to {viewModelType.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Navigation] ERROR: Failed to resolve {viewModelType.Name}. Exception: {ex}");
            throw; // Rethrow to be caught by ShellViewModel
        }

        if (CurrentViewModel is INavigable navigableTo)
        {
            navigableTo.OnNavigatedTo(null);
        }
    }

    public void GoBack()
    {
    }
}
