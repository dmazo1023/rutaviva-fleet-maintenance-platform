using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.References;

public sealed class ApproverReference : Entity<ApproverId>
{
    private ApproverReference()
    {
        FullName = null!;
        Email = null!;
    }

    public CustomerId CustomerId { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public ReferenceStatus Status { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == ReferenceStatus.Active;

    public static ApproverReference Create(ApproverId id, CustomerId customerId, string fullName, string email, ReferenceStatus status, DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            CustomerId = customerId,
            FullName = fullName,
            Email = email,
            Status = status,
            UpdatedAt = updatedAt
        };

    public void Apply(CustomerId customerId, string fullName, string email, ReferenceStatus status, DateTimeOffset updatedAt)
    {
        CustomerId = customerId;
        FullName = fullName;
        Email = email;
        Apply(status, updatedAt);
    }

    public void Apply(ReferenceStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }
}
