namespace Mass.Spec.Contracts.ServiceBus;

/// <summary>
/// Marker interface for requests that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
public interface IRequest<TResponse>
{
}

/// <summary>
/// Marker interface for requests that do not return a response (commands).
/// </summary>
public interface IRequest : IRequest<Unit>
{
}

/// <summary>
/// Represents a void/unit return type for commands.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}

/// <summary>
/// Handles a request and returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle.</typeparam>
/// <typeparam name="TResponse">The type of response to return.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a command (request with no return value).
/// </summary>
/// <typeparam name="TRequest">The type of command to handle.</typeparam>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit>
{
}
