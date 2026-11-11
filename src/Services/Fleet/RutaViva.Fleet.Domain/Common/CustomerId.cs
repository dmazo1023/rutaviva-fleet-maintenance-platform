using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Domain.Common;

public readonly record struct CustomerId
{
    public CustomerId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static CustomerId New() => new(Guid.CreateVersion7());

    public static Result<CustomerId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return VehicleErrors.IdentifierEmpty;
        }

        return new CustomerId(value);
    }

    public override string ToString() => Value.ToString();
}
