using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain;

public sealed class Approver : Entity<ApproverId>
{
    private Approver()
    {
    }

    public CustomerId CustomerId { get; private set; }

    public PersonName FullName { get; private set; } = null!;

    public EmailAddress Email { get; private set; } = null!;

    public ApproverStatus Status { get; private set; }

    internal static Approver Create(CustomerId customerId, PersonName fullName, EmailAddress email) =>
        new()
        {
            Id = ApproverId.New(),
            CustomerId = customerId,
            FullName = fullName,
            Email = email,
            Status = ApproverStatus.Active
        };

    internal bool ChangeStatus(ApproverStatus target)
    {
        if (Status == target)
        {
            return false;
        }

        Status = target;
        return true;
    }
}
