using Mass.Spec.Contracts.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Mass.Core.Messaging;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType);
        
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for {requestType.Name}");
        }

        // Invoke Handle method via reflection since we don't know the exact generic type at compile time easily here without dynamic
        // A cleaner way is to use a wrapper, but for simplicity:
        var method = handlerType.GetMethod("Handle");
        if (method == null)
        {
             throw new InvalidOperationException($"Handler {handlerType.Name} is invalid");
        }

        var task = (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
        return task;
    }
}
