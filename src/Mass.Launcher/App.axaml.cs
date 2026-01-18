using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Mass.Launcher.Services;
using Mass.Launcher.ViewModels;
using Mass.Launcher.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Mass.Launcher;

public partial class App : Application
{
    public IHost? Host { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashWindow = new SplashWindow();
            desktop.MainWindow = splashWindow;
            splashWindow.Show();

            try
            {
                var bootstrapper = new Bootstrapper(status =>
                {
                    Dispatcher.UIThread.Post(() => splashWindow.UpdateStatus(status));
                });

                Host = await bootstrapper.InitializeAsync();

                var shellViewModel = Host.Services.GetRequiredService<ShellViewModel>();
                var navigationService = Host.Services.GetRequiredService<Mass.Core.Abstractions.INavigationService>();
                
                // Navigate to home by default
                navigationService.NavigateTo<HomeViewModel>();

                var mainWindow = new MainWindow { DataContext = shellViewModel };
                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                splashWindow.Close();

                desktop.Exit += (s, e) => Host?.Dispose();
            }
            catch (Exception ex)
            {
                // GLOBAL ERROR TRAP: Ensure the splash screen shows the error and doesn't just disappear
                // This prevents "silent death" on startup
                System.Diagnostics.Trace.WriteLine($"Startup Fatal Error: {ex}");
                
                // If possible, updating the splash with the error is best
                Dispatcher.UIThread.Post(() => 
                {
                    splashWindow.ShowError($"CRITICAL STARTUP ERROR:\n{ex.Message}");
                    // Keep window open so user can see it
                });
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}

