using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Domain.Common;

public readonly record struct RoutineTypeId
{
    public RoutineTypeId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static RoutineTypeId New() => new(Guid.CreateVersion7());

    public static Result<RoutineTypeId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return MaintenanceErrors.IdentifierEmpty;
        }

        return new RoutineTypeId(value);
    }

    public override string ToString() => Value.ToString();
}
