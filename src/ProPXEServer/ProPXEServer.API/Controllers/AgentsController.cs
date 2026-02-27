using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProPXEServer.API.Data;
using Mass.Spec.Contracts.Agent;
using Asp.Versioning;

namespace ProPXEServer.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(ApplicationDbContext db, ILogger<AgentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AgentRegistrationRequest request)
    {
        try
        {
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.MacAddress == request.MacAddress);
            if (agent == null)
            {
                agent = new Agent
                {
                    MacAddress = request.MacAddress,
                    Hostname = request.Hostname,
                    Version = request.AgentVersion,
                    Capabilities = System.Text.Json.JsonSerializer.Serialize(request.Capabilities),
                    Status = "Online",
                    LastHeartbeat = DateTime.UtcNow
                };
                _db.Agents.Add(agent);
            }
            else
            {
                agent.Hostname = request.Hostname;
                agent.Version = request.AgentVersion;
                agent.Capabilities = System.Text.Json.JsonSerializer.Serialize(request.Capabilities);
                agent.Status = "Online";
                agent.LastHeartbeat = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Agent registered: {Hostname} ({Mac})", request.Hostname, request.MacAddress);

            return Ok(new AgentRegistrationResponse
            {
                AgentId = agent.Id.ToString(),
                AuthToken = Guid.NewGuid().ToString(), // Placeholder token
                HeartbeatIntervalSeconds = 30
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent");
            return StatusCode(500, "Error registering agent");
        }
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] AgentHeartbeatRequest request)
    {
        try
        {
            var agent = await _db.Agents.FindAsync(request.AgentId);
            if (agent == null)
            {
                return NotFound();
            }

            agent.LastHeartbeat = DateTime.UtcNow;
            agent.Status = request.Status;
            
            await _db.SaveChangesAsync();

            // Logic to fetch pending jobs would go here
            // For now, return empty job
            return Ok(new AgentHeartbeatResponse
            {
                Success = true,
                PendingJob = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing heartbeat");
            return StatusCode(500, "Error processing heartbeat");
        }
    }
}
