using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Mass.Core.Abstractions;
using Mass.Launcher.Views;
using System.Threading.Tasks;

namespace Mass.Launcher.Services;

public class DialogService : IDialogService
{
    public async Task ShowErrorDialogAsync(string title, string message, bool canRetry = false)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new ErrorDialog
            {
                Title = title,
                Message = message,
                ShowRetryButton = canRetry
            };
            
            await dialog.ShowDialog(desktop.MainWindow);
        }
    }

    public async Task ShowMessageDialogAsync(string title, string message)
    {
        // Reuse ErrorDialog for now or create a MessageDialog
        await ShowErrorDialogAsync(title, message, false);
    }
}
