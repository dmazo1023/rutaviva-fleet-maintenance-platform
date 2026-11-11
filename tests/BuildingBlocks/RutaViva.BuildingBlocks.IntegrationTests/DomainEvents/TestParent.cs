using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class TestParent : AggregateRoot<Guid>
{
    private TestParent()
    {
    }

    public DateTimeOffset CreatedAt { get; private set; }

    public static TestParent Register(Guid id, DateTimeOffset createdAt)
    {
        TestParent parent = new() { Id = id, CreatedAt = createdAt };
        parent.Raise(new ParentRegistered(id, createdAt));
        return parent;
    }
}
