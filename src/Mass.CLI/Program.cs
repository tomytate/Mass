using System.CommandLine;
using Mass.CLI;
using Mass.CLI.Commands;
using Spectre.Console;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mass.Core.Extensions;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Core.Telemetry;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add Mass.Core services
        services.AddMassCoreServices();
        
        // Add logging - CLI uses console + file
        var fileLog = new FileLogService();
        var consoleLog = new ConsoleLogService();
        services.AddSingleton<ILogService>(new CompositeLogService(new ILogService[] { fileLog, consoleLog }));
        
        // Add telemetry
        services.AddSingleton<ITelemetryService, LocalTelemetryService>();
        
        // Conditionally register ProUSB if available
        try
        {
            var proUsbType = Type.GetType("ProUSB.ProUsbModule, ProUSB");
            if (proUsbType != null)
            {
                var addServicesMethod = proUsbType.GetMethod("AddServices", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                addServicesMethod?.Invoke(null, new object[] { services });
            }
        }
        catch { /* Module not available */ }
        
        // Register CLI commands
        services.AddSingleton<BurnCommand>();
        services.AddSingleton<WorkflowCommand>();
        services.AddSingleton<ConfigCommand>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogService>();
logger.LogInformation("Mass CLI started", "CLI");

// Display banner
AnsiConsole.Write(
    new FigletText("Mass Suite")
        .Color(Color.Blue));

// Build root command
var rootCommand = new RootCommand("Mass Suite CLI - Professional Deployment & Media Creation Tool");

rootCommand.AddCommand(host.Services.GetRequiredService<BurnCommand>());
rootCommand.AddCommand(host.Services.GetRequiredService<WorkflowCommand>());
rootCommand.AddCommand(host.Services.GetRequiredService<ConfigCommand>());

try 
{
    return await rootCommand.InvokeAsync(args);
}
catch (Mass.Spec.Exceptions.OperationException opEx)
{
    logger.LogCritical($"Operation failed: {opEx.Message}", opEx, "CLI");
    return Mass.CLI.Services.ExitCodeMapper.Map(opEx.Error);
}
catch (Exception ex)
{
    logger.LogCritical("Unhandled exception in CLI", ex, "CLI");
    return ExitCodes.GeneralError;
}
