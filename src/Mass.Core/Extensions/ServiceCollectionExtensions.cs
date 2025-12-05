using Mass.Core.Configuration;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Core.Registry;
using Mass.Core.Services;
using Mass.Core.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Mass.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMassCoreServices(this IServiceCollection services)
    {
        // Core infrastructure - RegistryService as concrete class (no interface)
        services.AddSingleton<RegistryService>();
        
        services.AddSingleton<IConfigurationService>(sp =>
        {
            var logService = sp.GetService<ILogService>() ?? new FileLogService();
            return new JsonConfigurationService(logService);
        });

        // Workflow engine
        services.AddSingleton<IWorkflowExecutor>(sp =>
        {
            var logService = sp.GetRequiredService<ILogService>();
            return new WorkflowExecutor(logService);
        });

        // IPC service
        services.AddSingleton<IIpcService, IpcService>();

        // Note: ILogService should be registered by the consuming application
        // as it may need different sinks (Console, File, Telemetry, Composite)

        // Note: IUsbBurner and IPxeManager are intentionally NOT registered here
        // as their concrete implementations live in ProUSB and ProPXEServer respectively

        return services;
    }

    public static IServiceCollection AddFileLogging(this IServiceCollection services)
    {
        services.AddSingleton<ILogService, FileLogService>();
        return services;
    }

    public static IServiceCollection AddConsoleLogging(this IServiceCollection services)
    {
        services.AddSingleton<ILogService, ConsoleLogService>();
        return services;
    }

    public static IServiceCollection AddCompositeLogging(this IServiceCollection services, params ILogService[] sinks)
    {
        services.AddSingleton<ILogService>(sp => new CompositeLogService(sinks));
        return services;
    }
}
