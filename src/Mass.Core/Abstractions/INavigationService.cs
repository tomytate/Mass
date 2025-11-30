namespace Mass.Core.Abstractions;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : UI.ViewModelBase;
    void NavigateTo(Type viewModelType);
    void GoBack();
    bool CanGoBack { get; }
}
