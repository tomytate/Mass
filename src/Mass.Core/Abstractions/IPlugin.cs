using Mass.Spec.Contracts.Plugins;

namespace Mass.Core.Abstractions;

public interface IPlugin
{
    PluginManifest Manifest { get; }
    void Init(IServiceProvider services);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
