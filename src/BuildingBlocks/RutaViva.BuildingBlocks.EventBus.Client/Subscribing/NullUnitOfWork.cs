using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class NullUnitOfWork : IUnitOfWork
{
    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}
