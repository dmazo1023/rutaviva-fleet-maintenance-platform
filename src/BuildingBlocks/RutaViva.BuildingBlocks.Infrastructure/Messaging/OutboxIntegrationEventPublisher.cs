using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxIntegrationEventPublisher(DbContext context, OutboxMessageFactory messageFactory) : IIntegrationEventPublisher
{
    public void Enqueue(IIntegrationEvent integrationEvent, string subject)
    {
        OutboxMessage message = messageFactory.Create(integrationEvent, subject);
        context.Set<OutboxMessage>().Add(message);
    }
}
