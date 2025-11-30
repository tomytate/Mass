using System.Threading.Tasks;
using Avalonia.Controls;
using ProUSB.UI.Views;

namespace ProUSB.UI.ViewModels;

public static class BurnSummaryHelper {
    public static async Task ShowSummaryAsync(
        Window owner,
        string deviceName,
        string isoName,
        string fileSystem,
        string partitionScheme,
        int persistenceMB) {
        
        var dialog = new Window {
            Title = "Burn Complete",
            Width = 500,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(24) };

        panel.Children.Add(new TextBlock {
            Text = "✅ Burn Operation Complete!",
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Foreground = Avalonia.Media.Brushes.LightGreen,
            Margin = new Avalonia.Thickness(0, 0, 0, 16)
        });

        panel.Children.Add(new TextBlock {
            Text = "Your USB drive is ready to use.",
            FontSize = 14,
            Foreground = Avalonia.Media.Brushes.Gray,
            Margin = new Avalonia.Thickness(0, 0, 0, 24)
        });

        panel.Children.Add(new TextBlock {
            Text = "Summary",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Avalonia.Thickness(0, 0, 0, 12)
        });

        panel.Children.Add(new TextBlock {
            Text = $"Device: {deviceName}",
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        });

        panel.Children.Add(new TextBlock {
            Text = $"ISO: {System.IO.Path.GetFileName(isoName)}",
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        });

        panel.Children.Add(new TextBlock {
            Text = $"File System: {fileSystem.ToUpper()}",
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        });

        panel.Children.Add(new TextBlock {
            Text = $"Partition: {partitionScheme}",
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        });

        panel.Children.Add(new TextBlock {
            Text = $"Persistence: {(persistenceMB > 0 ? $"{persistenceMB} MB" : "Disabled")}",
            Margin = new Avalonia.Thickness(0, 0, 0, 24)
        });

        panel.Children.Add(new TextBlock {
            Text = "Next Steps",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Avalonia.Thickness(0, 0, 0, 12)
        });

        panel.Children.Add(new TextBlock {
            Text = "1. Safely eject your USB drive\n" +
                   "2. Insert it into your target computer\n" +
                   "3. Boot from USB (usually F12/F2/DEL at startup)\n" +
                   "4. Follow on-screen installation instructions",
            Margin = new Avalonia.Thickness(0, 0, 0, 24)
        });

        var closeButton = new Button {
            Content = "CLOSE",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Padding = new Avalonia.Thickness(20, 10),
            Background = Avalonia.Media.Brushes.DodgerBlue,
            Foreground = Avalonia.Media.Brushes.White
        };
        closeButton.Click += (s, e) => dialog.Close();
        panel.Children.Add(closeButton);

        dialog.Content = new ScrollViewer { Content = panel };

        await dialog.ShowDialog(owner);
    }
}


