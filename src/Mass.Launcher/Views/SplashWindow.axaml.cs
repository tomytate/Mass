using Avalonia.Controls;
using Avalonia.Media;

namespace Mass.Launcher.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string status)
    {
        if (StatusText != null)
        {
            StatusText.Text = status;
        }
    }

    public void ShowError(string error)
    {
        if (StatusText != null)
        {
            StatusText.Text = error;
            StatusText.Foreground = Brushes.Red;
        }
    }
}
