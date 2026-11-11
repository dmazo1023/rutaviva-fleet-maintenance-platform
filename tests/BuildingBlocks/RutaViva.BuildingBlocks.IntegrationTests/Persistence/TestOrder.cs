using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

public sealed class TestOrder : IHasRowVersion
{
    public TestOrder(Guid id, string name, TestOrderStatus status, DateTimeOffset createdAt, decimal amount)
    {
        Id = id;
        Name = name;
        Status = status;
        CreatedAt = createdAt;
        Amount = amount;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public TestOrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public decimal Amount { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public void Rename(string name)
    {
        Name = name;
    }
}
