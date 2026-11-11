using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Customers.Domain.Common;

public readonly record struct ApproverId
{
    public ApproverId(Guid value) => Value = Guard.NotDefault(value);

    public Guid Value { get; }

    public static ApproverId New() => new(Guid.CreateVersion7());

    public static Result<ApproverId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return CustomerErrors.IdentifierEmpty;
        }

        return new ApproverId(value);
    }

    public override string ToString() => Value.ToString();
}
