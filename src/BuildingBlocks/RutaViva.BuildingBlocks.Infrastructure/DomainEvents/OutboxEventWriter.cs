using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public sealed class OutboxEventWriter(IServiceProvider serviceProvider, OutboxMessageFactory messageFactory)
{
    private static readonly ClosedGenericCache<IIntegrationEventMapperInvoker> Mappers = new(typeof(IntegrationEventMapperInvoker<>));

    public void Write(DbContext context, IReadOnlyList<CollectedDomainEvent> events)
    {
        DbSet<OutboxMessage> outbox = context.Set<OutboxMessage>();
        foreach (CollectedDomainEvent collected in events)
        {
            outbox.AddRange(CreateMessages(collected));
        }
    }

    private List<OutboxMessage> CreateMessages(CollectedDomainEvent collected)
    {
        IIntegrationEventMapperInvoker mapper = Mappers.For(collected.Event.GetType());
        IReadOnlyCollection<IIntegrationEvent> integrationEvents = mapper.Map(serviceProvider, collected.Event);
        return integrationEvents.Select(integrationEvent => messageFactory.Create(integrationEvent, collected.Subject)).ToList();
    }
}
