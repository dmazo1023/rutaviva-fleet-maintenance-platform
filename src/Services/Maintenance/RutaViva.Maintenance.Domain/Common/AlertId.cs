using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Domain.Common;

public readonly record struct AlertId
{
    public AlertId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static AlertId New() => new(Guid.CreateVersion7());

    public static Result<AlertId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return MaintenanceErrors.IdentifierEmpty;
        }

        return new AlertId(value);
    }

    public override string ToString() => Value.ToString();
}
