using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed record ParentRegistered(Guid ParentId, DateTimeOffset OccurredAt) : IDomainEvent;
