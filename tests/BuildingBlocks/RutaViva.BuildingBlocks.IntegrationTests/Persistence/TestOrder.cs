using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

public sealed class TestOrder : AggregateRoot<Guid>
{
    public TestOrder(Guid id, string name, TestOrderStatus status, DateTimeOffset createdAt, decimal amount)
    {
        Id = id;
        Name = name;
        Status = status;
        CreatedAt = createdAt;
        Amount = amount;
    }

    public string Name { get; private set; }

    public TestOrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public decimal Amount { get; private set; }

    public void Rename(string name)
    {
        Name = name;
    }
}
