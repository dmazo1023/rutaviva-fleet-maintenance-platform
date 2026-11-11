using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class FailingEffectHandler(AttemptCounter counter) : IIntegrationEventHandler<FailingHappenedV1>
{
    private const string FailureMessage = "The handler always throws.";

    public Task<Result> HandleAsync(FailingHappenedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        counter.Increment();
        throw new InvalidOperationException(FailureMessage);
    }
}
