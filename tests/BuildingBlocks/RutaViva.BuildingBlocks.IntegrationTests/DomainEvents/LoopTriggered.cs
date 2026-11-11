using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed record LoopTriggered(Guid LoopId, DateTimeOffset OccurredAt) : IDomainEvent;
