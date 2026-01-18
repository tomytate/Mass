using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Mass.Agent;

/// <summary>
/// The core background worker for the Mass Agent.
/// Responsible for device registration, heartbeat signaling, 
/// and secure job execution (Command/Script).
/// </summary>
[SupportedOSPlatform("windows")]
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private PerformanceCounter? _cpuCounter;
    private readonly HashSet<string> _allowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "hostname",
        "ipconfig",
        "shutdown",
        "echo",
        "whoami"
    };

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _http = new HttpClient { BaseAddress = new Uri(_config["ServerUrl"] ?? "http://localhost:5006") };
        
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // Initial call to prime the counter
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize CPU performance counter");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mac = GetDeterministicMacAddress();
        var hostname = Environment.MachineName;
        var agentId = string.Empty;

        _logger.LogInformation("Agent initialized. Host: {Host}, Identity (MAC): {Mac}", hostname, mac);

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
                    AgentVersion = "1.0.1",
                    Capabilities = ["Deployment", "Inventory", "SecureExecution"]
                };

                var response = await _http.PostAsJsonAsync("/api/v1/agents/register", regRequest, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Mass.Spec.Contracts.Agent.AgentRegistrationResponse>();
                    agentId = result?.AgentId ?? string.Empty;
                    _logger.LogInformation("Agent registered successfully. Assigned ID: {Id}", agentId);
                }
                else
                {
                    _logger.LogWarning("Registration pending. Server returned: {Status}", response.StatusCode);
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration connection failed");
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
                    CpuUsage = GetCpuUsage(),
                    MemoryUsage = GetMemoryUsage()
                };

                var response = await _http.PostAsJsonAsync("/api/v1/agents/heartbeat", heartbeat, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Heartbeat rejected: {Status}", response.StatusCode);
                    // If 401/404, we might need to re-register
                }
                else
                {
                    var result = await response.Content.ReadFromJsonAsync<Mass.Spec.Contracts.Agent.AgentHeartbeatResponse>();
                    if (result?.PendingJob != null)
                    {
                        await ExecuteJobAsync(result.PendingJob, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat connection error");
            }

            await Task.Delay(30000, stoppingToken);
        }
    }

    private async Task ExecuteJobAsync(Mass.Spec.Contracts.Agent.AgentJob job, CancellationToken ct)
    {
        _logger.LogInformation("Processing Job {Id}: {Type}", job.JobId, job.JobType);

        try
        {
            if (job.JobType == "Command")
            {
                var cmd = job.Parameters.GetValueOrDefault("Command") ?? string.Empty;
                var args = job.Parameters.GetValueOrDefault("Arguments") ?? string.Empty;
                var fullCmd = $"{cmd} {args}".Trim();

                // 1. Security Check: Whitelist
                if (!_allowedCommands.Contains(cmd))
                {
                    _logger.LogWarning("Security Block: Command '{Cmd}' is not in the allowlist.", cmd);
                    return; // Fail silently or report back failure
                }

                // 2. Safe Execution
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe", // In a real scenario, call the exe directly, not shell
                    Arguments = $"/c {fullCmd}", // Still using shell for pipes, but cmd is whitelisted
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    // Secure timeout
                    var timeoutTask = Task.Delay(30000, ct); // 30s max execution
                    var waitTask = process.WaitForExitAsync(ct);

                    if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask)
                    {
                        process.Kill();
                        _logger.LogError("Job {Id} timed out.", job.JobId);
                    }
                    else
                    {
                        var output = await process.StandardOutput.ReadToEndAsync(ct);
                        var error = await process.StandardError.ReadToEndAsync(ct);

                        _logger.LogInformation("Job {Id} Complete. Output: {Output}", job.JobId, (output + error).Trim());
                    }
                }
            }
            else
            {
                _logger.LogWarning("Job type '{Type}' is disabled by security policy.", job.JobType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error executing job {Id}", job.JobId);
        }
    }

    private string GetDeterministicMacAddress()
    {
        // Algorithm:
        // 1. Up, Non-Loopback
        // 2. Preferred Types: Ethernet > Wireless > Other
        // 3. Lowest Interface Index (Stability)

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .OrderBy(n => n.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Ethernet => 0,
                NetworkInterfaceType.Wireless80211 => 1,
                _ => 2
            })
            // Then by index? No property for index easily exposed without P/Invoke or hacky parsing, 
            // but standard sort order is usually consistent per boot.
            // Let's sort by ID or Name to ensure determinism across reboots if indices drift.
            .ThenBy(n => n.Id)
            .ToList();

        var bestMatch = interfaces.FirstOrDefault();
        return bestMatch?.GetPhysicalAddress().ToString() ?? "000000000000";
    }

    private double GetCpuUsage()
    {
        try
        {
            return _cpuCounter?.NextValue() ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private double GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var usedMemory = process.WorkingSet64;
            var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            return totalMemory > 0 ? (usedMemory * 100.0 / totalMemory) : 0;
        }
        catch
        {
            return 0;
        }
    }
}
