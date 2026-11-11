using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.Vehicles.ValueObjects;

public sealed record Kilometers : ValueObject
{
    private Kilometers(long value) => Value = value;

    public long Value { get; }

    public static Result<Kilometers> Create(long value)
    {
        if (value < 0)
        {
            return VehicleErrors.InvalidOdometer;
        }

        return new Kilometers(value);
    }

    public override string ToString() => Value.ToString();
}
