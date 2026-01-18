using Microsoft.AspNetCore.SignalR.Client;

namespace Mass.Agent;

public class HeartbeatService : BackgroundService
{
    private readonly ILogger<HeartbeatService> _logger;
    private readonly AgentConfiguration _config;
    private HubConnection? _hubConnection;

    public HeartbeatService(ILogger<HeartbeatService> logger, AgentConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // Wait for main worker to connect first
        
        var retryDelay = TimeSpan.FromSeconds(_config.HeartbeatIntervalSeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
                {
                    await ConnectToHub(stoppingToken);
                }

                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    var heartbeat = new
                    {
                        AgentId = _config.AgentId,
                        Timestamp = DateTime.UtcNow,
                        Status = "Online",
                        CpuUsage = GetCpuUsage(),
                        MemoryUsage = GetMemoryUsage(),
                        UptimeSeconds = (long)(DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalSeconds
                    };

                    await _hubConnection.InvokeAsync("Heartbeat", heartbeat, stoppingToken);
                    _logger.LogDebug("Heartbeat sent");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Heartbeat failed");
            }

            await Task.Delay(retryDelay, stoppingToken);
        }
    }

    private async Task ConnectToHub(CancellationToken stoppingToken)
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_config.DashboardUrl}/hubs/agents")
                .WithAutomaticReconnect()
                .Build();

            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("Heartbeat service connected");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to connect heartbeat service");
        }
    }

    private static double GetCpuUsage()
    {
        // Approximate CPU usage based on process time vs elapsed time
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
        var elapsed = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds;
        if (elapsed <= 0) return 0;
        return Math.Min(100, (cpuTime / (elapsed * Environment.ProcessorCount)) * 100);
    }

    private static long GetMemoryUsage()
    {
        return Environment.WorkingSet / 1024 / 1024; // MB
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
