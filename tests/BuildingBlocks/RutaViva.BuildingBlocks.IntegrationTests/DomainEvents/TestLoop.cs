using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class TestLoop : AggregateRoot<Guid>
{
    private TestLoop()
    {
    }

    public DateTimeOffset StartedAt { get; private set; }

    public static TestLoop Start(Guid id, DateTimeOffset startedAt)
    {
        TestLoop loop = new() { Id = id, StartedAt = startedAt };
        loop.Raise(new LoopTriggered(id, startedAt));
        return loop;
    }
}
