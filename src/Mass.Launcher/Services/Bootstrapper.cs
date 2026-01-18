using System.Reflection;
using Mass.Core.Abstractions;
using Mass.Core.Configuration;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Core.Plugins;
using Mass.Core.Scripting;
using Mass.Core.Services;
using Mass.Core.Telemetry;
using Mass.Core.UI;
using Mass.Core.Updates;
using Mass.Launcher.Services;
using Mass.Launcher.ViewModels;
using Mass.Launcher.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProUSB;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using FluentValidation;

namespace Mass.Launcher.Services;

public class Bootstrapper
{
    private readonly Action<string> _statusCallback;
    private IHost? _host;

    public Bootstrapper(Action<string> statusCallback)
    {
        _statusCallback = statusCallback;
    }

    public async Task<IHost> InitializeAsync()
    {
        _statusCallback("Initializing services...");

        var builder = Host.CreateApplicationBuilder();

        ConfigureServices(builder.Services);

        _statusCallback("Building host...");
        _host = builder.Build();

        await StartServicesAsync(_host);

        return _host;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Path Configuration
        var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "settings.json");
        var localesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Locales");
        Directory.CreateDirectory(localesPath);

        // -- Core Services --
        
        // Logging
        var fileLogService = new FileLogService();
        services.AddSingleton<IConfigurationService>(sp => new JsonConfigurationService(fileLogService, configPath));
        
        services.AddSingleton<ITelemetryService, LocalTelemetryService>();
        services.AddSingleton<Mass.Core.Events.IEventBus, Mass.Core.Events.EventBus>();
        services.AddSingleton<Mass.Core.Messaging.IMediator, Mass.Core.Messaging.Mediator>();
        services.AddSingleton<Mass.Core.Messaging.IMediator, Mass.Core.Messaging.Mediator>();
        services.AddSingleton<Mass.Core.State.IApplicationState, Mass.Core.State.ApplicationState>();
        services.AddSingleton<Mass.Core.Services.INotificationService, Mass.Core.Services.NotificationService>();

        // Validation
        services.AddValidatorsFromAssemblyContaining<Mass.Core.Validation.AgentRegistrationRequestValidator>();

        // Caching
        services.AddMemoryCache();
        // Redis optional
        // var redisConn = Environment.GetEnvironmentVariable("MASS_REDIS");
        // if (!string.IsNullOrEmpty(redisConn))
        // {
        //     services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
        // }
        
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .AddSource("Mass.Launcher")
                    .AddSource("Mass.Core")
                    .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("Mass.Launcher"))
                    .AddConsoleExporter();
            })
            .WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder
                    .AddRuntimeInstrumentation()
                    .AddConsoleExporter();
            });

        services.AddHealthChecks()
            .AddCheck<Mass.Core.Health.PxeServicesHealthCheck>("pxe")
            .AddCheck<Mass.Core.Health.PluginHealthCheck>("plugins");

        services.AddSingleton<ILogService>(sp =>
        {
            var telemetry = sp.GetRequiredService<ITelemetryService>();
            var telemetryLog = new TelemetryLogService(telemetry);
            return new CompositeLogService(new ILogService[] { fileLogService, telemetryLog });
        });

        // Application Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IActivityService, ActivityService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ResourceAlertService>();
        services.AddSingleton<Mass.Core.Security.IElevationService, Mass.Core.Security.ElevationService>();
        services.AddSingleton<Mass.Core.Security.ICredentialService, Mass.Core.Security.CredentialService>();
        services.AddSingleton<Mass.Core.Security.IAuditService, Mass.Core.Security.AuditService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IRollbackService, RollbackService>();
        services.AddSingleton<IOperationsConsoleService, OperationsConsoleService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IIpcService, IpcService>(); // Ensure ProPXEServer is decoupled if possible, or just IPC
        
        // Status Service (Upgraded dependencies)
        services.AddSingleton<IStatusService, StatusService>();

        // Scripting
        services.AddSingleton<IScriptingService, LuaScriptingService>();
        services.AddTransient<ScriptingViewModel>();

        // Localization
        var localizationService = new JsonLocalizationService(localesPath);
        services.AddSingleton<ILocalizationService>(localizationService);

        // -- UI Services --
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HealthViewModel>();
        services.AddTransient<PluginsViewModel>();
        services.AddTransient<WorkflowsViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<OperationsConsoleViewModel>();
        services.AddSingleton<CommandPaletteViewModel>();
        services.AddTransient<ConsentDialogViewModel>();
        services.AddTransient<OnboardingViewModel>();
        services.AddSingleton<ToastViewModel>();

        // -- Modules / Plugins --
        // ProUSB - Using extension method from ProUSB core
        services.AddProUsb();
        
        // FORCE LOAD ProUSB Assembly for ViewLocator
        // This ensures AppDomain.CurrentDomain.GetAssemblies() includes ProUSB when ViewLocator scans.
        var proUsbViewType = typeof(ProUSB.UI.Views.MainView);
        services.AddSingleton(new AssemblyReferenceHolder(proUsbViewType.Assembly));

        // Dynamic Plugins
        ConfigurePluginInterfaces(services);
    }

    // Helper to keep assembly alive/loaded
    public record AssemblyReferenceHolder(Assembly Assembly);

    private void ConfigurePluginInterfaces(IServiceCollection services)
    {
        // Standard plugin paths
        var appDataPlugins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite", "plugins");
        var localPlugins = Path.Combine(AppContext.BaseDirectory, "plugins");

        var pluginPaths = new List<string> { appDataPlugins, localPlugins };

        // Robust Dev Discovery: Check sibling build outputs for both Debug/Release
        // This allows running the Launcher and picking up plugins without manual copy steps
        var baseDir = AppContext.BaseDirectory;
        var configName = baseDir.Contains("Debug") ? "Debug" : "Release";
        
        // Path to specific plugin output directories relative to Launcher output
        // assuming standard src/Project/bin/Config/net10.0 layout
        var devPxePath = Path.GetFullPath(Path.Combine(baseDir, "../../../../ProPXEServer/ProPXEServer.Plugin/bin", configName, "net10.0"));
        
        if (Directory.Exists(devPxePath))
        {
            pluginPaths.Add(devPxePath);
            System.Diagnostics.Debug.WriteLine($"[Bootstrapper] Added dev plugin path: {devPxePath}");
        }

        var pluginLoader = new PluginLoader();
        var pluginDiscovery = new PluginDiscoveryService(pluginPaths.ToArray());

        services.AddSingleton<IPluginLoader>(pluginLoader);
        services.AddSingleton(pluginDiscovery);
        services.AddSingleton<PluginLifecycleManager>();
    }

    private async Task StartServicesAsync(IHost host)
    {
        _statusCallback("Loading configuration...");
        var configService = host.Services.GetRequiredService<IConfigurationService>();
        // Check if configService is specifically the Json one to call LoadAsync, 
        // or add LoadAsync to interface? The interface likely doesn't have it, casting safely.
        if (configService is JsonConfigurationService jsonConfig)
        {
            await jsonConfig.LoadAsync();
        }

        _statusCallback("Initializing Localization...");
        Services.Localizer.Instance.Initialize(host.Services.GetRequiredService<ILocalizationService>());

        // Initialize Plugins
        var pluginLifecycle = host.Services.GetRequiredService<PluginLifecycleManager>();
        await pluginLifecycle.InitializeAsync();

        _statusCallback("Discovering plugins...");
        var pluginDiscovery = host.Services.GetRequiredService<PluginDiscoveryService>();
        var discoveredPlugins = await pluginDiscovery.DiscoverPluginsAsync();
        
        var pluginList = discoveredPlugins.ToList();
        for (int i = 0; i < pluginList.Count; i++)
        {
            var plugin = pluginList[i];
            _statusCallback($"Loading plugin... ({i + 1}/{pluginList.Count})");
            try
            {
                await pluginLifecycle.LoadPluginAsync(plugin);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load plugin: {ex.Message}");
            }
        }

        _statusCallback("Starting services...");
        await host.StartAsync();

        // Perform Initial Navigation
        var navigationService = host.Services.GetRequiredService<INavigationService>();
        var settings = configService.Get<Mass.Spec.Config.GeneralSettings>("General");

        if (settings.IsFirstRun)
        {
            _statusCallback("Preparing first-run experience...");
            navigationService.NavigateTo<OnboardingViewModel>();
        }
        else
        {
            navigationService.NavigateTo<HomeViewModel>();
        }

        _statusCallback("Ready!");
    }
}
