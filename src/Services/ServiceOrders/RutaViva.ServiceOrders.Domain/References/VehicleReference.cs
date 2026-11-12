using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.ServiceOrders.Domain.References;

public sealed class VehicleReference : Entity<Guid>
{
    private VehicleReference()
    {
        Plate = null!;
    }

    public string Plate { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid WorkshopId { get; private set; }

    public Guid? ResponsibleApproverId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static VehicleReference Create(
        Guid id,
        string plate,
        Guid customerId,
        Guid workshopId,
        Guid? responsibleApproverId,
        bool isActive,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            Plate = plate,
            CustomerId = customerId,
            WorkshopId = workshopId,
            ResponsibleApproverId = responsibleApproverId,
            IsActive = isActive,
            UpdatedAt = updatedAt
        };

    public void Apply(VehicleReference source)
    {
        Plate = source.Plate;
        CustomerId = source.CustomerId;
        WorkshopId = source.WorkshopId;
        ResponsibleApproverId = source.ResponsibleApproverId;
        IsActive = source.IsActive;
        UpdatedAt = source.UpdatedAt;
    }

    public void ChangeStatus(bool isActive, DateTimeOffset updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    public void AssignResponsible(Guid? responsibleApproverId, DateTimeOffset updatedAt)
    {
        ResponsibleApproverId = responsibleApproverId;
        UpdatedAt = updatedAt;
    }
}
