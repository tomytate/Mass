using Mass.Dashboard;
using Mass.Dashboard.Components;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Core.Workflows;
using Mass.Core.Configuration;
using Mass.Core.Telemetry;
using Mass.Spec.Contracts.Agent;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Mass.Core Services
// ============================================================================
builder.Services.AddSingleton<ILogService, FileLogService>();

var configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
    "MassSuite", 
    "settings.json");

builder.Services.AddSingleton<IConfigurationService>(sp => 
    new JsonConfigurationService(sp.GetRequiredService<ILogService>(), configPath));

// Workflow Services
builder.Services.AddSingleton<WorkflowParser>();
builder.Services.AddSingleton<WorkflowValidator>();
builder.Services.AddSingleton<WorkflowExecutor>();

// Telemetry
builder.Services.AddSingleton<ITelemetryService, LocalTelemetryService>();

// Agent State (in-memory for now, could be persisted)
builder.Services.AddSingleton<AgentRegistry>();

// ============================================================================
// Blazor & API Services
// ============================================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ============================================================================
// Middleware Pipeline
// ============================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRouting();

// ============================================================================
// Agent Management API Endpoints
// ============================================================================
var agentApi = app.MapGroup("/api/v1/agents").RequireAuthorization();

agentApi.MapPost("/register", (AgentRegistrationRequest request, AgentRegistry registry, ILogService log) =>
{
    var agentId = registry.RegisterAgent(request);
    log.LogInformation($"Agent registered: {request.Hostname} -> {agentId}");
    return Results.Ok(new AgentRegistrationResponse { AgentId = agentId });
});

agentApi.MapPost("/heartbeat", (AgentHeartbeatRequest request, AgentRegistry registry) =>
{
    var job = registry.ProcessHeartbeat(request);
    return Results.Ok(new AgentHeartbeatResponse 
    { 
        Success = true, 
        PendingJob = job 
    });
});

agentApi.MapGet("/", (AgentRegistry registry) =>
{
    return Results.Ok(registry.GetAllAgents());
});

agentApi.MapGet("/{agentId}", (string agentId, AgentRegistry registry) =>
{
    var agent = registry.GetAgent(agentId);
    return agent != null ? Results.Ok(agent) : Results.NotFound();
});

// ============================================================================
// Workflow API Endpoints
// ============================================================================
var workflowApi = app.MapGroup("/api/v1/workflows");

workflowApi.MapGet("/", (WorkflowParser parser) =>
{
    var workflowsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MassSuite", "workflows");
    
    if (!Directory.Exists(workflowsPath))
        return Results.Ok(Array.Empty<string>());
    
    var files = Directory.GetFiles(workflowsPath, "*.yaml");
    return Results.Ok(files.Select(Path.GetFileNameWithoutExtension));
});

// ============================================================================
// Blazor Components
// ============================================================================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapHub<AgentHub>("/hubs/agents");

app.Run();

// ============================================================================
// Supporting Classes
// ============================================================================

/// <summary>
/// In-memory registry for connected agents.
/// </summary>
public class AgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentState> _agents = new();
    private readonly ConcurrentQueue<AgentJob> _pendingJobs = new();

    public string RegisterAgent(AgentRegistrationRequest request)
    {
        var agentId = $"agent-{Guid.NewGuid():N}"[..16];
        _agents[agentId] = new AgentState
        {
            AgentId = agentId,
            Hostname = request.Hostname,
            MacAddress = request.MacAddress,
            OsVersion = request.OsVersion,
            AgentVersion = request.AgentVersion,
            Status = "Online",
            LastSeen = DateTime.UtcNow,
            RegisteredAt = DateTime.UtcNow
        };
        return agentId;
    }

    public AgentJob? ProcessHeartbeat(AgentHeartbeatRequest request)
    {
        if (_agents.TryGetValue(request.AgentId, out var agent))
        {
            agent.Status = request.Status;
            agent.CpuUsage = request.CpuUsage;
            agent.MemoryUsage = request.MemoryUsage;
            agent.LastSeen = DateTime.UtcNow;
        }

        // Return pending job if any
        return _pendingJobs.TryDequeue(out var job) ? job : null;
    }

    public AgentState? GetAgent(string agentId) =>
        _agents.TryGetValue(agentId, out var agent) ? agent : null;

    public IEnumerable<AgentState> GetAllAgents() => _agents.Values;

    public void QueueJob(AgentJob job) => _pendingJobs.Enqueue(job);
}

public class AgentState
{
    public string AgentId { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string AgentVersion { get; set; } = "";
    public string Status { get; set; } = "";
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// SignalR hub for real-time agent communication.
/// </summary>
public class AgentHub : Hub
{
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(ILogger<AgentHub> logger) => _logger = logger;

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Agent connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Agent disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendCommand(string agentId, string command)
    {
        await Clients.All.SendAsync("ReceiveCommand", agentId, command);
    }
}
