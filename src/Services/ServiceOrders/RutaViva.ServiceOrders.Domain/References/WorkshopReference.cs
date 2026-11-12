using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.ServiceOrders.Domain.References;

public sealed class WorkshopReference : Entity<Guid>
{
    private WorkshopReference()
    {
        Name = null!;
    }

    public string Name { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static WorkshopReference Create(Guid id, string name, DateTimeOffset updatedAt) =>
        new() { Id = id, Name = name, UpdatedAt = updatedAt };

    public void Apply(WorkshopReference source)
    {
        Name = source.Name;
        UpdatedAt = source.UpdatedAt;
    }
}
