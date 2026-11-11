using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class RejectingEffectHandler(AttemptCounter counter) : IIntegrationEventHandler<FailingHappenedV1>
{
    private const string FailureCode = "Testing.Rejected";
    private const string FailureMessage = "The handler rejects the event.";

    private static readonly Error Failure = new(FailureCode, FailureMessage, ErrorType.Validation);

    public Task<Result> HandleAsync(FailingHappenedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        counter.Increment();
        return Task.FromResult(Result.Failure(Failure));
    }
}
