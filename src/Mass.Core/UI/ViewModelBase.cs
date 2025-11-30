using CommunityToolkit.Mvvm.ComponentModel;

namespace Mass.Core.UI;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;
}
