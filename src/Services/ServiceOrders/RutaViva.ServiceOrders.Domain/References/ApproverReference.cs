using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.ServiceOrders.Domain.References;

public sealed class ApproverReference : Entity<Guid>
{
    private ApproverReference()
    {
        FullName = null!;
    }

    public Guid CustomerId { get; private set; }

    public string FullName { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ApproverReference Create(Guid id, Guid customerId, string fullName, bool isActive, DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            CustomerId = customerId,
            FullName = fullName,
            IsActive = isActive,
            UpdatedAt = updatedAt
        };

    public void Apply(ApproverReference source)
    {
        CustomerId = source.CustomerId;
        FullName = source.FullName;
        IsActive = source.IsActive;
        UpdatedAt = source.UpdatedAt;
    }

    public void ChangeStatus(bool isActive, DateTimeOffset updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    public ServiceOrderApprover ToApprover() => new(Id, CustomerId, FullName, IsActive);
}
