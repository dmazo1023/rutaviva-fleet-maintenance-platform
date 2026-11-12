using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.ServiceOrders.Domain.Common;

public readonly record struct ServiceOrderId
{
    public ServiceOrderId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static ServiceOrderId New() => new(Guid.CreateVersion7());

    public static Result<ServiceOrderId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return ServiceOrderErrors.IdentifierEmpty;
        }

        return new ServiceOrderId(value);
    }

    public override string ToString() => Value.ToString();
}
