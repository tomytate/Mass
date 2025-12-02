using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mass.Core.Abstractions;
using Mass.Core.Plugins;
using Mass.Core.Services;
using Mass.Core.Telemetry;
using Mass.Core.Scripting;
using Mass.Core.UI;
using Mass.Launcher.Services;
using Mass.Launcher.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Mass.Launcher;

public partial class App : Application
{
    public IHost? Host { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MassSuite",
            "settings.json");
        var configService = new Mass.Core.Configuration.JsonConfigurationService(configPath);
        builder.Services.AddSingleton<Mass.Core.Abstractions.IConfigurationService>(configService);

        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<Mass.Core.Services.IStatusService, Mass.Core.Services.StatusService>();
        builder.Services.AddSingleton<Mass.Core.Services.IActivityService, Mass.Core.Services.ActivityService>();
        builder.Services.AddSingleton<Mass.Core.Logging.ILogService, Mass.Core.Logging.FileLogService>();
        builder.Services.AddSingleton<Mass.Core.Services.INotificationService, Mass.Core.Services.NotificationService>();
        builder.Services.AddSingleton<Mass.Core.Services.ResourceAlertService>();
        builder.Services.AddSingleton<Mass.Core.Security.IElevationService, Mass.Core.Security.ElevationService>();
        builder.Services.AddSingleton<Mass.Core.Security.ICredentialService, Mass.Core.Security.CredentialService>();
        builder.Services.AddSingleton<Mass.Core.Updates.IUpdateService, Mass.Core.Updates.UpdateService>();
        builder.Services.AddSingleton<Mass.Core.Updates.IRollbackService, Mass.Core.Updates.RollbackService>();
        builder.Services.AddSingleton<Mass.Core.UI.IOperationsConsoleService, Mass.Core.UI.OperationsConsoleService>();
        builder.Services.AddSingleton<Mass.Core.UI.IOperationsConsoleService, Mass.Core.UI.OperationsConsoleService>();
        builder.Services.AddSingleton<Mass.Core.UI.ICommandPaletteService, Mass.Core.UI.CommandPaletteService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<Mass.Core.Services.IIpcService, Mass.Core.Services.IpcService>();
        
        // Telemetry
        builder.Services.AddSingleton<ITelemetryService, LocalTelemetryService>();
        builder.Services.AddTransient<ConsentDialogViewModel>();

        // Scripting
        builder.Services.AddSingleton<IScriptingService, LuaScriptingService>();
        builder.Services.AddTransient<ScriptingViewModel>();

        // Localization
        var localesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Locales");
        // Ensure directory exists for dev
        if (!Directory.Exists(localesPath))
        {
             Directory.CreateDirectory(localesPath);
        }
        var localizationService = new JsonLocalizationService(localesPath);
        builder.Services.AddSingleton<ILocalizationService>(localizationService);
        
        // ViewModels
        builder.Services.AddSingleton<ShellViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<HealthViewModel>();
        builder.Services.AddTransient<PluginsViewModel>();
        builder.Services.AddTransient<WorkflowsViewModel>();
        builder.Services.AddTransient<LogsViewModel>();
        builder.Services.AddTransient<OperationsConsoleViewModel>();
        builder.Services.AddSingleton<CommandPaletteViewModel>();

        var pluginPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "plugins"),
            Path.Combine(AppContext.BaseDirectory, "plugins"),
            // Add development paths
            Path.Combine(AppContext.BaseDirectory, "../../../ProUSB/bin/Debug/net10.0"),
            Path.Combine(AppContext.BaseDirectory, "../../../MassBoot/MassBoot.Plugin/bin/Debug/net10.0")
        };

        var pluginLoader = new PluginLoader();
        var pluginDiscovery = new PluginDiscoveryService(pluginPaths);
        var pluginLifecycle = new PluginLifecycleManager(pluginLoader);

        builder.Services.AddSingleton(pluginLoader);
        builder.Services.AddSingleton(pluginDiscovery);
        builder.Services.AddSingleton(pluginLifecycle);

        Host = builder.Build();
        
        configService.LoadAsync().GetAwaiter().GetResult();
        
        var discoveredPlugins = pluginDiscovery.DiscoverPluginsAsync().GetAwaiter().GetResult();
        foreach (var discoveredPlugin in discoveredPlugins)
        {
            pluginLifecycle.LoadPluginAsync(discoveredPlugin, Host.Services).GetAwaiter().GetResult();
        }
        
        Host.Start();

        // Initialize Localizer
        Services.Localizer.Instance.Initialize(Host.Services.GetRequiredService<ILocalizationService>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var shellViewModel = Host.Services.GetRequiredService<ShellViewModel>();
            var navigationService = Host.Services.GetRequiredService<INavigationService>();
            
            navigationService.NavigateTo<HomeViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = shellViewModel
            };
            
            desktop.Exit += (s, e) => Host.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
