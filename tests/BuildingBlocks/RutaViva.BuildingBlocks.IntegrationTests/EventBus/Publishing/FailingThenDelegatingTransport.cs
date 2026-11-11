using RutaViva.BuildingBlocks.EventBus.Client.Publishing;
using RutaViva.IntegrationContracts;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Publishing;

public sealed class FailingThenDelegatingTransport(IIntegrationEventTransport inner, int failuresBeforeDelegating) : IIntegrationEventTransport
{
    public const string FailureMessage = "Simulated transport failure";

    private int _calls;

    public Task<long> SendAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        _calls++;
        return _calls <= failuresBeforeDelegating
            ? Task.FromException<long>(new InvalidOperationException(FailureMessage))
            : inner.SendAsync(stream, envelope, cancellationToken);
    }
}
