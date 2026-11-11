using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class SkippedEventStrategy : IEventDispatchStrategy
{
    public static readonly SkippedEventStrategy Instance = new();

    private SkippedEventStrategy()
    {
    }

    public Task<Result> ExecuteAsync(IServiceProvider services, IntegrationEventEnvelope envelope, EventMetadata metadata, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());
}
