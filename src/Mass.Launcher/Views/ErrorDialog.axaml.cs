using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Mass.Launcher.Views;

public partial class ErrorDialog : Window
{
    public string Message
    {
        get => this.FindControl<TextBlock>("MessageText")?.Text ?? string.Empty;
        set
        {
            var textBlock = this.FindControl<TextBlock>("MessageText");
            if (textBlock != null) textBlock.Text = value;
        }
    }

    public bool ShowRetryButton
    {
        get => this.FindControl<Button>("RetryButton")?.IsVisible ?? false;
        set
        {
            var btn = this.FindControl<Button>("RetryButton");
            if (btn != null) btn.IsVisible = value;
        }
    }

    public ErrorDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnRetryClick(object sender, RoutedEventArgs e)
    {
        Close(true); // Return true for retry if needed, though ShowDialog returns void/result
    }
}
