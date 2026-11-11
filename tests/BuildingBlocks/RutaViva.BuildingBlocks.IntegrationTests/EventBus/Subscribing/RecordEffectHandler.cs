using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.IntegrationTests.Messaging;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class RecordEffectHandler(SubscribingTestDbContext context) : IIntegrationEventHandler<SampleHappenedV1>
{
    public Task<Result> HandleAsync(SampleHappenedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        context.Effects.Add(ProcessedEffect.From(metadata));
        return Task.FromResult(Result.Success());
    }
}
