using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class HandlerDispatchStrategy<TEvent, THandler> : IEventDispatchStrategy
    where TEvent : IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    public async Task<Result> ExecuteAsync(IServiceProvider services, IntegrationEventEnvelope envelope, EventMetadata metadata, CancellationToken cancellationToken)
    {
        TEvent? integrationEvent = envelope.Data.Deserialize<TEvent>(IntegrationEventJson.Options);
        if (integrationEvent is null)
        {
            return Result.Failure(SubscriberErrors.PayloadInvalid);
        }

        THandler handler = services.GetRequiredService<THandler>();
        return await handler.HandleAsync(integrationEvent, metadata, cancellationToken);
    }
}
