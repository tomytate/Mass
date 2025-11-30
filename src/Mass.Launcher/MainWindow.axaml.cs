using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Mass.Launcher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void CloseApp(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LaunchProUSB(object? sender, RoutedEventArgs e)
    {
        try
        {
            string exePath = "ProUSBMediaSuite.exe";
            
            if (!File.Exists(exePath))
            {
                exePath = Path.Combine("..", "ProUSBMediaSuite", "bin", "Debug", "net10.0", "ProUSBMediaSuite.exe");
            }

            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(exePath)),
                    UseShellExecute = true
                });
            }
            else
            {
                Debug.WriteLine("ProUSBMediaSuite.exe not found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error launching ProUSB: {ex.Message}");
        }
    }

    private async void LaunchMassBoot(object? sender, RoutedEventArgs e)
    {
        try
        {
            string apiPath = Path.Combine("ProPXEServer", "ProPXEServer.API.exe");
            
            if (!File.Exists(apiPath))
            {
                apiPath = Path.Combine("..", "ProPXEServer", "ProPXEServer.API", "bin", "Debug", "net10.0", "ProPXEServer.API.exe");
            }

            if (!File.Exists(apiPath))
            {
                apiPath = Path.Combine("..", "..", "src", "ProPXEServer", "ProPXEServer.API", "bin", "Debug", "net10.0", "ProPXEServer.API.exe");
            }

            if (!File.Exists(apiPath))
            {
                apiPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ProPXEServer", "ProPXEServer.API", "bin", "Debug", "net10.0", "ProPXEServer.API.exe");
            }

            var normalizedPath = Path.GetFullPath(apiPath);

            if (!File.Exists(normalizedPath))
            {
                Debug.WriteLine("ProPXEServer.API.exe not found");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = normalizedPath,
                WorkingDirectory = Path.GetDirectoryName(normalizedPath),
                UseShellExecute = true,
            });

            await Task.Delay(2000);

            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:5000/swagger",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error launching ProPXEServer: {ex.Message}");
        }
    }
}
