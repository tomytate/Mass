namespace Mass.Core.Abstractions;

public interface IService
{
    string ServiceName { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    ServiceStatus GetStatus();
}

public enum ServiceStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Failed
}
