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

        var viewModel = _serviceProvider.GetRequiredService(viewModelType) as ViewModelBase;
        if (viewModel == null)
        {
            throw new InvalidOperationException($"Could not resolve ViewModel of type {viewModelType.Name}");
        }

        CurrentViewModel = viewModel;

        if (CurrentViewModel is INavigable navigableTo)
        {
            navigableTo.OnNavigatedTo(null);
        }
    }

    public void GoBack()
    {
    }
}
