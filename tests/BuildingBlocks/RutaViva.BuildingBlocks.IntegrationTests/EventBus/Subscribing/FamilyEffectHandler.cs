using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class FamilyEffectHandler(SubscribingTestDbContext context) : IIntegrationEventHandler<IFamilyEvent>
{
    public Task<Result> HandleAsync(IFamilyEvent integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        context.Effects.Add(ProcessedEffect.From(metadata));
        return Task.FromResult(Result.Success());
    }
}
