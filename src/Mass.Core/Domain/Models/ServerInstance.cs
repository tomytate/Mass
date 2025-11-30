namespace Mass.Core.Domain.Models;

public sealed class ServerInstance : Entity<Guid>
{
    public string Name { get; private set; }
    public ServerType Type { get; private set; }
    public int Port { get; private set; }
    public ServerStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? StoppedAt { get; private set; }
    public int? ProcessId { get; private set; }

    private ServerInstance(Guid id, string name, ServerType type, int port) : base(id)
    {
        Name = name;
        Type = type;
        Port = port;
        Status = ServerStatus.Stopped;
    }

    public static ServerInstance Create(string name, ServerType type, int port)
    {
        var server = new ServerInstance(Guid.NewGuid(), name, type, port);
        server.RaiseDomainEvent(new ServerInstanceCreatedEvent(server.Id, name, type, port));
        return server;
    }

    public void Start(int processId)
    {
        Status = ServerStatus.Running;
        ProcessId = processId;
        StartedAt = DateTime.UtcNow;
        StoppedAt = null;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ServerInstanceStartedEvent(Id, processId));
    }

    public void Stop()
    {
        Status = ServerStatus.Stopped;
        ProcessId = null;
        StoppedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ServerInstanceStoppedEvent(Id));
    }

    public void MarkAsFailed(string reason)
    {
        Status = ServerStatus.Failed;
        StoppedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ServerInstanceFailedEvent(Id, reason));
    }
}

public enum ServerType
{
    PxeBoot,
    Tftp,
    Dhcp,
    Http
}

public enum ServerStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Failed
}

public sealed class ServerInstanceCreatedEvent : DomainEvent
{
    public Guid ServerId { get; }
    public string Name { get; }
    public ServerType Type { get; }
    public int Port { get; }

    public ServerInstanceCreatedEvent(Guid serverId, string name, ServerType type, int port)
    {
        ServerId = serverId;
        Name = name;
        Type = type;
        Port = port;
    }
}

public sealed class ServerInstanceStartedEvent : DomainEvent
{
    public Guid ServerId { get; }
    public int ProcessId { get; }

    public ServerInstanceStartedEvent(Guid serverId, int processId)
    {
        ServerId = serverId;
        ProcessId = processId;
    }
}

public sealed class ServerInstanceStoppedEvent : DomainEvent
{
    public Guid ServerId { get; }

    public ServerInstanceStoppedEvent(Guid serverId)
    {
        ServerId = serverId;
    }
}

public sealed class ServerInstanceFailedEvent : DomainEvent
{
    public Guid ServerId { get; }
    public string Reason { get; }

    public ServerInstanceFailedEvent(Guid serverId, string reason)
    {
        ServerId = serverId;
        Reason = reason;
    }
}
