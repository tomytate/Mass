namespace Mass.Core.Domain.Models;

public sealed class Deployment : Entity<Guid>
{
    public string Name { get; private set; }
    public DeploymentType Type { get; private set; }
    public DeploymentStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Deployment(Guid id, string name, DeploymentType type) : base(id)
    {
        Name = name;
        Type = type;
        Status = DeploymentStatus.Pending;
    }

    public static Deployment Create(string name, DeploymentType type)
    {
        var deployment = new Deployment(Guid.NewGuid(), name, type);
        deployment.RaiseDomainEvent(new DeploymentCreatedEvent(deployment.Id, name, type));
        return deployment;
    }

    public void Start()
    {
        if (Status != DeploymentStatus.Pending)
            throw new InvalidOperationException($"Cannot start deployment in {Status} status");

        Status = DeploymentStatus.Running;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DeploymentStartedEvent(Id));
    }

    public void Complete()
    {
        if (Status != DeploymentStatus.Running)
            throw new InvalidOperationException($"Cannot complete deployment in {Status} status");

        Status = DeploymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DeploymentCompletedEvent(Id));
    }

    public void Fail(string errorMessage)
    {
        Status = DeploymentStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DeploymentFailedEvent(Id, errorMessage));
    }
}

public enum DeploymentType
{
    UsbProvisioning,
    PxeBoot,
    Combined
}

public enum DeploymentStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed class DeploymentCreatedEvent : DomainEvent
{
    public Guid DeploymentId { get; }
    public string Name { get; }
    public DeploymentType Type { get; }

    public DeploymentCreatedEvent(Guid deploymentId, string name, DeploymentType type)
    {
        DeploymentId = deploymentId;
        Name = name;
        Type = type;
    }
}

public sealed class DeploymentStartedEvent : DomainEvent
{
    public Guid DeploymentId { get; }

    public DeploymentStartedEvent(Guid deploymentId)
    {
        DeploymentId = deploymentId;
    }
}

public sealed class DeploymentCompletedEvent : DomainEvent
{
    public Guid DeploymentId { get; }

    public DeploymentCompletedEvent(Guid deploymentId)
    {
        DeploymentId = deploymentId;
    }
}

public sealed class DeploymentFailedEvent : DomainEvent
{
    public Guid DeploymentId { get; }
    public string ErrorMessage { get; }

    public DeploymentFailedEvent(Guid deploymentId, string errorMessage)
    {
        DeploymentId = deploymentId;
        ErrorMessage = errorMessage;
    }
}
