using Mass.Core.Interfaces;
using Mass.Core.Workflows;
using Mass.Spec.Contracts.Workflow;
using Microsoft.AspNetCore.SignalR.Client;

namespace Mass.Agent;

public class AgentWorker : BackgroundService
{
    private readonly ILogger<AgentWorker> _logger;
    private readonly AgentConfiguration _config;
    private readonly ILogService _logService;
    private readonly WorkflowExecutor _executor;
    private readonly WorkflowQueue _workflowQueue;
    private HubConnection? _hubConnection;

    public AgentWorker(ILogger<AgentWorker> logger, AgentConfiguration config, ILogService logService)
    {
        _logger = logger;
        _config = config;
        _logService = logService;
        _executor = new WorkflowExecutor(logService);
        _workflowQueue = new WorkflowQueue();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Mass Agent starting: {AgentId} ({AgentName})", _config.AgentId, _config.AgentName);
        
        await ConnectToHub(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingWorkflows(stoppingToken);
            await Task.Delay(1000, stoppingToken);
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

            _hubConnection.On<WorkflowDefinition>("ExecuteWorkflow", async (workflow) =>
            {
                _logger.LogInformation("Received workflow to execute: {WorkflowName}", workflow.Name);
                var result = await _executor.ExecuteAsync(workflow);
                
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("WorkflowCompleted", _config.AgentId, workflow.Id, result.Success);
                }
            });

            _hubConnection.On<string, Dictionary<string, object>>("ExecuteCommand", async (command, parameters) =>
            {
                _logger.LogInformation("Received command: {Command}", command);
                await ExecuteCommand(command, parameters);
            });

            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("Connected to dashboard hub");
            
            await _hubConnection.InvokeAsync("RegisterAgent", new
            {
                AgentId = _config.AgentId,
                AgentName = _config.AgentName,
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                Tags = _config.Tags
            }, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to dashboard. Will retry on next heartbeat.");
        }
    }

    private async Task ProcessPendingWorkflows(CancellationToken stoppingToken)
    {
        // Process queued workflows from local persistent queue
        var queued = _workflowQueue?.Dequeue();
        if (queued is null) return;

        _logger.LogInformation("Processing queued workflow: {Name} (queued {Time})", 
            queued.Workflow.Name, queued.QueuedAt);

        try
        {
            var result = await _executor.ExecuteAsync(queued.Workflow, ct: stoppingToken);

            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("WorkflowCompleted", 
                    _config.AgentId, queued.Workflow.Id, result.Success, stoppingToken);
            }

            _logger.LogInformation("Queued workflow completed: {Name} - Success: {Success}", 
                queued.Workflow.Name, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute queued workflow: {Name}", queued.Workflow.Name);

            // Re-queue with retry logic
            if (queued.RetryCount < 3)
            {
                queued.RetryCount++;
                _workflowQueue?.Enqueue(queued.Workflow);
            }
        }
    }

    private async Task ExecuteCommand(string command, Dictionary<string, object> parameters)
    {
        switch (command.ToLowerInvariant())
        {
            case "restart":
                _logger.LogInformation("Restart command received");
                break;
            case "update":
                _logger.LogInformation("Update command received");
                break;
            case "collect-inventory":
                await CollectInventory();
                break;
            default:
                _logger.LogWarning("Unknown command: {Command}", command);
                break;
        }
    }

    private async Task CollectInventory()
    {
        var inventory = new
        {
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            SystemDirectory = Environment.SystemDirectory,
            UserName = Environment.UserName,
            WorkingSet = Environment.WorkingSet,
            CollectedAt = DateTime.UtcNow
        };

        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("InventoryCollected", _config.AgentId, inventory);
        }

        _logger.LogInformation("Inventory collected and sent to dashboard");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mass Agent stopping");
        
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
