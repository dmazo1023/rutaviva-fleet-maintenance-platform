using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.Approvers;

public sealed class AddApproverHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<AddApproverCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddApproverCommand command, CancellationToken cancellationToken)
    {
        Customer? customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        Result<ApproverId> approver = AddApprover(customer, command);
        if (approver.IsFailure)
        {
            return approver.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return approver.Value.Value;
    }

    private Result<ApproverId> AddApprover(Customer customer, AddApproverCommand command)
    {
        Result<PersonName> name = PersonName.Create(command.FullName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        Result<EmailAddress> email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error;
        }

        return customer.AddApprover(name.Value, email.Value, clock.UtcNow);
    }
}
