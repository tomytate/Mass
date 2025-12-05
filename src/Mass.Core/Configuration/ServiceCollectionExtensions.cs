using Mass.Core.Configuration;
using Mass.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mass.Core.Configuration;

/// <summary>
/// Extension methods for registering configuration services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Mass Suite configuration service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configPath">Optional custom path for the configuration file.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMassConfiguration(
        this IServiceCollection services, 
        string? configPath = null)
    {
        services.AddSingleton<IConfigurationService>(sp => 
        {
            var logger = sp.GetRequiredService<ILogService>();
            return new JsonConfigurationService(logger, configPath);
        });

        return services;
    }
}
