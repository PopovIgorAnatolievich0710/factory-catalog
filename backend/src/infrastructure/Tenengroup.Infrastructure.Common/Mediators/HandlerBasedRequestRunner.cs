using Microsoft.Extensions.DependencyInjection;

namespace Tenengroup.Infrastructure.Common.Mediators;

public class HandlerBasedRequestRunner(IServiceProvider serviceProvider) : IRequestRunner
{
    public Task Run<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest>>()
                      ?? throw new InvalidOperationException($"Handler is not found for request: {typeof(TRequest).Name}");

        return handler.Handle(request, cancellationToken);
    }

    public Task<TResult> Run<TRequest, TResult>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResult>
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResult>>()
                      ?? throw new InvalidOperationException($"Handler is not found for request: {typeof(TRequest).Name}");

        return handler.Handle(request, cancellationToken);
    }
}
