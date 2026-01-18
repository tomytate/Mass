using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mass.Core.Security;

/// <summary>
/// Service for recording security-related audit events.
/// </summary>
public interface IAuditService
{
    void LogEvent(AuditEvent auditEvent);
    Task LogEventAsync(AuditEvent auditEvent, CancellationToken ct = default);
    IEnumerable<AuditEvent> GetRecentEvents(int count = 100);
}

/// <summary>
/// Represents a security audit event.
/// </summary>
public record AuditEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..12];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required string EventType { get; init; }
    public required string UserId { get; init; }
    public string? IpAddress { get; init; }
    public string? Resource { get; init; }
    public string? Action { get; init; }
    public bool Success { get; init; } = true;
    public string? Details { get; init; }
}

/// <summary>
/// Static event types for consistent audit logging.
/// </summary>
public static class AuditEventTypes
{
    public const string Login = "login";
    public const string Logout = "logout";
    public const string LoginFailed = "login_failed";
    public const string TokenRefresh = "token_refresh";
    public const string ApiAccess = "api_access";
    public const string ApiAccessDenied = "api_access_denied";
    public const string ConfigChange = "config_change";
    public const string DataExport = "data_export";
    public const string UserCreated = "user_created";
    public const string UserDeleted = "user_deleted";
    public const string PermissionChange = "permission_change";
}

/// <summary>
/// File-based audit service with structured JSON logging.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly string _auditLogPath;
    private readonly List<AuditEvent> _recentEvents = [];
    private readonly Lock _lock = new();
    private const int MaxRecentEvents = 1000;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
        var auditDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MassSuite", "Audit");
        Directory.CreateDirectory(auditDir);
        _auditLogPath = Path.Combine(auditDir, $"audit_{DateTime.UtcNow:yyyyMMdd}.jsonl");
    }

    public void LogEvent(AuditEvent auditEvent)
    {
        lock (_lock)
        {
            _recentEvents.Add(auditEvent);
            if (_recentEvents.Count > MaxRecentEvents)
                _recentEvents.RemoveAt(0);
        }

        var json = JsonSerializer.Serialize(auditEvent);
        
        try
        {
            File.AppendAllLines(_auditLogPath, [json]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry");
        }

        _logger.LogInformation(
            "AUDIT: {EventType} | User: {UserId} | Resource: {Resource} | Success: {Success}",
            auditEvent.EventType, auditEvent.UserId, auditEvent.Resource, auditEvent.Success);
    }

    public async Task LogEventAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _recentEvents.Add(auditEvent);
            if (_recentEvents.Count > MaxRecentEvents)
                _recentEvents.RemoveAt(0);
        }

        var json = JsonSerializer.Serialize(auditEvent);

        try
        {
            await File.AppendAllLinesAsync(_auditLogPath, [json], ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry");
        }

        _logger.LogInformation(
            "AUDIT: {EventType} | User: {UserId} | Resource: {Resource} | Success: {Success}",
            auditEvent.EventType, auditEvent.UserId, auditEvent.Resource, auditEvent.Success);
    }

    public IEnumerable<AuditEvent> GetRecentEvents(int count = 100)
    {
        lock (_lock)
        {
            return _recentEvents.TakeLast(count).ToList();
        }
    }
}
