using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.Events;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain;

public sealed class Customer : AggregateRoot<CustomerId>
{
    private readonly List<Approver> _approvers = [];

    private Customer()
    {
    }

    public PersonName Name { get; private set; } = null!;

    public TaxId TaxId { get; private set; } = null!;

    public EmailAddress Email { get; private set; } = null!;

    public PhoneNumber? Phone { get; private set; }

    public CustomerStatus Status { get; private set; }

    public IReadOnlyCollection<Approver> Approvers => _approvers.AsReadOnly();

    public static Customer Register(PersonName name, TaxId taxId, EmailAddress email, PhoneNumber? phone, DateTimeOffset at)
    {
        Guard.NotNull(name);
        Guard.NotNull(taxId);
        Guard.NotNull(email);

        Customer customer = new()
        {
            Id = CustomerId.New(),
            Name = name,
            TaxId = taxId,
            Email = email,
            Phone = phone,
            Status = CustomerStatus.Active
        };

        customer.Raise(new CustomerRegistered(customer.Id, taxId, name, email, customer.Status, at));
        return customer;
    }

    public Result<ApproverId> AddApprover(PersonName name, EmailAddress email, DateTimeOffset at)
    {
        Guard.NotNull(name);
        Guard.NotNull(email);

        if (Status != CustomerStatus.Active)
        {
            return CustomerErrors.Inactive;
        }

        if (HasApproverWithEmail(email))
        {
            return CustomerErrors.ApproverEmailAlreadyRegistered;
        }

        Approver approver = Approver.Create(Id, name, email);
        _approvers.Add(approver);
        Raise(new ApproverRegistered(approver.Id, Id, name, email, approver.Status, at));
        return approver.Id;
    }

    public Result Activate(DateTimeOffset at) => ChangeStatus(CustomerStatus.Active, at);

    public Result Deactivate(DateTimeOffset at) => ChangeStatus(CustomerStatus.Inactive, at);

    public Result ActivateApprover(ApproverId approverId, DateTimeOffset at) =>
        ChangeApproverStatus(approverId, ApproverStatus.Active, at);

    public Result DeactivateApprover(ApproverId approverId, DateTimeOffset at) =>
        ChangeApproverStatus(approverId, ApproverStatus.Inactive, at);

    private bool HasApproverWithEmail(EmailAddress email) =>
        _approvers.Any(approver => approver.Email == email);

    private Result ChangeStatus(CustomerStatus target, DateTimeOffset at)
    {
        if (Status == target)
        {
            return Result.Success();
        }

        Status = target;
        Raise(new CustomerStatusChanged(Id, target, at));
        return Result.Success();
    }

    private Result ChangeApproverStatus(ApproverId approverId, ApproverStatus target, DateTimeOffset at)
    {
        Approver? approver = _approvers.Find(candidate => candidate.Id == approverId);
        if (approver is null)
        {
            return Result.Failure(CustomerErrors.ApproverNotFound);
        }

        if (!approver.ChangeStatus(target))
        {
            return Result.Success();
        }

        Raise(new ApproverStatusChanged(approver.Id, Id, target, at));
        return Result.Success();
    }
}
