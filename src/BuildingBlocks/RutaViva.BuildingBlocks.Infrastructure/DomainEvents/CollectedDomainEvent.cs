using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public sealed record CollectedDomainEvent(string Subject, IDomainEvent Event);
