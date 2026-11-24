namespace Tenengroup.Infrastructure.Common.Mediators;

public interface IRequestRunner
{
    Task Run<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    Task<TResult> Run<TRequest, TResult>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResult>;
}
