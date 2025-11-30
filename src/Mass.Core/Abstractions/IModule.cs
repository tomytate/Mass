using Mass.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Mass.Core.Abstractions;

public interface IModule
{
    PluginManifest Manifest { get; }
    void RegisterServices(IServiceCollection services);
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    Task ActivateAsync(CancellationToken cancellationToken = default);
    Task DeactivateAsync(CancellationToken cancellationToken = default);
    Task UnloadAsync(CancellationToken cancellationToken = default);
}
