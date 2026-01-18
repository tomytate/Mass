using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Linq;

namespace Mass.Agent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    
    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _http = new HttpClient { BaseAddress = new Uri(_config["ServerUrl"] ?? "http://localhost:5000") };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mac = GetMacAddress();
        var hostname = Environment.MachineName;
        var agentId = string.Empty;

        _logger.LogInformation("Agent starting. Host: {Host}, Mac: {Mac}", hostname, mac);

        // Registration Loop
        while (!stoppingToken.IsCancellationRequested && string.IsNullOrEmpty(agentId))
        {
            try
            {
                var regRequest = new Mass.Spec.Contracts.Agent.AgentRegistrationRequest
                {
                    Hostname = hostname,
                    MacAddress = mac,
                    OsVersion = Environment.OSVersion.ToString(),
                    AgentVersion = "1.0.0",
                    Capabilities = new List<string> { "Deployment", "Inventory" }
                };

                var response = await _http.PostAsJsonAsync("/api/v1/agents/register", regRequest, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Mass.Spec.Contracts.Agent.AgentRegistrationResponse>();
                    agentId = result?.AgentId ?? string.Empty;
                    _logger.LogInformation("Agent registered successfully. ID: {Id}", agentId);
                }
                else
                {
                    _logger.LogWarning("Registration failed. Status: {Status}", response.StatusCode);
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                await Task.Delay(5000, stoppingToken);
            }
        }

        // Heartbeat Loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var heartbeat = new Mass.Spec.Contracts.Agent.AgentHeartbeatRequest
                {
                    AgentId = agentId,
                    Status = "Idle",
                    CpuUsage = 0, // Implement actual metric gathering
                    MemoryUsage = 0
                };

                var response = await _http.PostAsJsonAsync("/api/v1/agents/heartbeat", heartbeat, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Heartbeat failed: {Status}", response.StatusCode);
                }
                else
                {
                    var result = await response.Content.ReadFromJsonAsync<Mass.Spec.Contracts.Agent.AgentHeartbeatResponse>();
                    if (result?.PendingJob != null)
                    {
                        _logger.LogInformation("Received job: {JobId} ({Type})", result.PendingJob.JobId, result.PendingJob.JobType);
                        // TODO: Execute job
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat error");
            }

            await Task.Delay(30000, stoppingToken);
        }
    }

    private string GetMacAddress()
    {
        return System.Net.NetworkInformation.NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
            .Select(n => n.GetPhysicalAddress().ToString())
            .FirstOrDefault() ?? "000000000000";
    }
}
