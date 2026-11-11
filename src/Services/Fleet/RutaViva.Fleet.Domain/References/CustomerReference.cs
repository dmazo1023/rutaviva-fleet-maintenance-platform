using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.References;

public sealed class CustomerReference : Entity<CustomerId>
{
    private CustomerReference()
    {
        TaxId = null!;
        Name = null!;
    }

    public string TaxId { get; private set; }

    public string Name { get; private set; }

    public ReferenceStatus Status { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == ReferenceStatus.Active;

    public static CustomerReference Create(CustomerId id, string taxId, string name, ReferenceStatus status, DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TaxId = taxId,
            Name = name,
            Status = status,
            UpdatedAt = updatedAt
        };

    public void Apply(string taxId, string name, ReferenceStatus status, DateTimeOffset updatedAt)
    {
        TaxId = taxId;
        Name = name;
        Apply(status, updatedAt);
    }

    public void Apply(ReferenceStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }
}
