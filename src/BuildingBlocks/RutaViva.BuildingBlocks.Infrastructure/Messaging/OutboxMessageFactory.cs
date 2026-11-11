using System.Text.Json;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxMessageFactory(IntegrationEventEnvelopeFactory envelopeFactory)
{
    public OutboxMessage Create(IIntegrationEvent integrationEvent, string subject)
    {
        IntegrationEventEnvelope envelope = envelopeFactory.Create(integrationEvent, subject);
        IntegrationEventDescriptor descriptor = IntegrationEventDescriptor.For(integrationEvent.GetType());
        string payload = JsonSerializer.Serialize(envelope, IntegrationEventJson.Options);

        return new OutboxMessage(envelope.Id, descriptor.Stream, envelope.Type, envelope.Subject, payload, envelope.Time);
    }
}
