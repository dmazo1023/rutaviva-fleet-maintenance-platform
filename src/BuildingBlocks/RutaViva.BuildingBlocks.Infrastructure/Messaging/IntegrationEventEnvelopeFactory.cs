using System.Diagnostics;
using System.Text.Json;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class IntegrationEventEnvelopeFactory
{
    private const string SourceFormat = "/rutaviva/{0}";

    private readonly IClock _clock;
    private readonly string _serviceName;

    public IntegrationEventEnvelopeFactory(IClock clock, IntegrationEventSettings settings)
    {
        _clock = Guard.NotNull(clock);
        _serviceName = Guard.NotNullOrWhiteSpace(Guard.NotNull(settings).ServiceName);
    }

    public IntegrationEventEnvelope Create(IIntegrationEvent integrationEvent, string subject)
    {
        Guard.NotNull(integrationEvent);
        Guard.NotNullOrWhiteSpace(subject);
        Type eventType = integrationEvent.GetType();
        IntegrationEventDescriptor descriptor = IntegrationEventDescriptor.For(eventType);

        return new IntegrationEventEnvelope
        {
            Id = Guid.NewGuid(),
            Source = string.Format(SourceFormat, _serviceName),
            Type = descriptor.EventType.ToString(),
            Subject = subject,
            Time = _clock.UtcNow,
            TraceParent = Activity.Current?.Id,
            Data = JsonSerializer.SerializeToElement(integrationEvent, eventType, IntegrationEventJson.Options),
        };
    }
}
