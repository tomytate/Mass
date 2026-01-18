using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mass.Core.Services;
using Mass.Core.UI;

namespace Mass.Launcher.ViewModels;

public partial class ToastViewModel : ViewModelBase
{
    private readonly INotificationService _notificationService;

    public ObservableCollection<ToastItemViewModel> Toasts { get; } = [];

    public ToastViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
        
        // If NotificationService exposes an event
        if (_notificationService is NotificationService ns)
        {
            ns.NotificationReceived += OnNotificationReceived;
        }
    }

    private void OnNotificationReceived(object? sender, Notification e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            var vm = new ToastItemViewModel(e);
            Toasts.Add(vm);
            
            // Auto-dismiss
            Task.Delay(5000).ContinueWith(_ => 
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() => Toasts.Remove(vm));
            });
        });
    }
    
    [RelayCommand]
    public void Dismiss(ToastItemViewModel item)
    {
        Toasts.Remove(item);
    }
}

public partial class ToastItemViewModel : ObservableObject
{
    public string Title { get; }
    public string Message { get; }
    public NotificationSeverity Severity { get; }
    
    public ToastItemViewModel(Notification n)
    {
        Title = n.Title;
        Message = n.Message;
        Severity = n.Severity;
    }

    public string BackgroundClass => Severity switch
    {
         NotificationSeverity.Error => "Danger",
         NotificationSeverity.Warning => "Warning",
         NotificationSeverity.Success => "Success",
         _ => "Info"
    };
}
