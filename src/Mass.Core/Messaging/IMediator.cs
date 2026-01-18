using Mass.Spec.Contracts.ServiceBus;

namespace Mass.Core.Messaging;

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
