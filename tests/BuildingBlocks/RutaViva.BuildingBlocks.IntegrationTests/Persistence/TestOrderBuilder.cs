namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

public sealed class TestOrderBuilder
{
    private const string DefaultName = "order";
    private const decimal DefaultAmount = 150.75m;

    private static readonly DateTimeOffset DefaultCreatedAt = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private Guid _id = Guid.NewGuid();
    private string _name = DefaultName;

    public TestOrderBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TestOrder Build() => new(_id, _name, TestOrderStatus.Pending, DefaultCreatedAt, DefaultAmount);
}
