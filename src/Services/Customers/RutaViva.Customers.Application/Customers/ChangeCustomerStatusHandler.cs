using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Customers;

public sealed class ChangeCustomerStatusHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ChangeCustomerStatusCommand, CustomerId>
{
    private static readonly IReadOnlyDictionary<CustomerStatus, Func<Customer, DateTimeOffset, Result>> Transitions =
        new Dictionary<CustomerStatus, Func<Customer, DateTimeOffset, Result>>
        {
            [CustomerStatus.Active] = (customer, at) => customer.Activate(at),
            [CustomerStatus.Inactive] = (customer, at) => customer.Deactivate(at)
        };

    public async Task<Result<CustomerId>> HandleAsync(ChangeCustomerStatusCommand command, CancellationToken cancellationToken)
    {
        Customer? customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        Result changed = Transitions[command.TargetStatus](customer, clock.UtcNow);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.CustomerId;
    }
}
