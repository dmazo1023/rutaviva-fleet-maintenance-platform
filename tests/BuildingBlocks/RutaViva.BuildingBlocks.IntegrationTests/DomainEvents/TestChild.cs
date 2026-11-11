using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class TestChild : AggregateRoot<Guid>
{
    public TestChild(Guid id)
    {
        Id = id;
    }
}
