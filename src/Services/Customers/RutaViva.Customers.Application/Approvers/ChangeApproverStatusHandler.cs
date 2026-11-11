using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Approvers;

public sealed class ChangeApproverStatusHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ChangeApproverStatusCommand, ApproverId>
{
    private static readonly IReadOnlyDictionary<ApproverStatus, Func<Customer, ApproverId, DateTimeOffset, Result>> Transitions =
        new Dictionary<ApproverStatus, Func<Customer, ApproverId, DateTimeOffset, Result>>
        {
            [ApproverStatus.Active] = (customer, approverId, at) => customer.ActivateApprover(approverId, at),
            [ApproverStatus.Inactive] = (customer, approverId, at) => customer.DeactivateApprover(approverId, at)
        };

    public async Task<Result<ApproverId>> HandleAsync(ChangeApproverStatusCommand command, CancellationToken cancellationToken)
    {
        Customer? customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        Result changed = Transitions[command.TargetStatus](customer, command.ApproverId, clock.UtcNow);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.ApproverId;
    }
}
