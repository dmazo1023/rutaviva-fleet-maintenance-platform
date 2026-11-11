using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed record ParentRegisteredV1(Guid ParentId) : IIntegrationEvent
{
    public const string EventName = "ParentRegistered";

    public const int Version = 1;

    public const string Stream = "testing.parent";
}
