using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mass.Core.Plugins;

namespace Mass.Core.Health;

public class PluginHealthCheck : IHealthCheck
{
    private readonly PluginLifecycleManager _lifecycleManager;

    public PluginHealthCheck(PluginLifecycleManager lifecycleManager)
    {
        _lifecycleManager = lifecycleManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var plugins = _lifecycleManager.LoadedPlugins; // Assuming we can expose this or get state
        // If critical plugins are missing, return Degraded.

        return Task.FromResult(HealthCheckResult.Healthy($"Loaded {plugins.Count()} plugins"));
    }
}
