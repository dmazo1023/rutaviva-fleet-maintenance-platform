using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Domain.Common;

public readonly record struct RoutineId
{
    public RoutineId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static RoutineId New() => new(Guid.CreateVersion7());

    public static Result<RoutineId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return MaintenanceErrors.IdentifierEmpty;
        }

        return new RoutineId(value);
    }

    public override string ToString() => Value.ToString();
}
