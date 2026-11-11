using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Domain.Common;

public readonly record struct VehicleId
{
    public VehicleId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static VehicleId New() => new(Guid.CreateVersion7());

    public static Result<VehicleId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return MaintenanceErrors.IdentifierEmpty;
        }

        return new VehicleId(value);
    }

    public override string ToString() => Value.ToString();
}
