namespace Mass.Spec.Contracts.Events;

/// <summary>
/// Marker interface for all domain events in the system.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base implementation of IEvent with common properties.
/// </summary>
public abstract record EventBase : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
