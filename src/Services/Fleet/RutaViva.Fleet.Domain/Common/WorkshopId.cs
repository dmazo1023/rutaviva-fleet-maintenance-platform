using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Domain.Common;

public readonly record struct WorkshopId
{
    public WorkshopId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static WorkshopId New() => new(Guid.CreateVersion7());

    public static Result<WorkshopId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return VehicleErrors.IdentifierEmpty;
        }

        return new WorkshopId(value);
    }

    public override string ToString() => Value.ToString();
}
