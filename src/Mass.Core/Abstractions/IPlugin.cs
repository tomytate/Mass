namespace Mass.Core.Abstractions;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    Task<PluginCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

public sealed record PluginMetadata(
    string Id,
    string Name,
    string Version,
    string Author,
    string Description,
    string? IconPath = null,
    IReadOnlyList<string>? Dependencies = null);

[Flags]
public enum PluginCapabilities
{
    None = 0,
    ProvidesTools = 1 << 0,
    ProvidesWorkflows = 1 << 1,
    ProvidesHealthChecks = 1 << 2,
    ProvidesUI = 1 << 3,
    ProvidesCLI = 1 << 4
}
