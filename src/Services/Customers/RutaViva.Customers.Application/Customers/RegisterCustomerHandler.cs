using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.Customers;

public sealed class RegisterCustomerHandler(
    ICustomerRepository customers,
    ICustomerUniquenessChecker uniqueness,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<RegisterCustomerCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        Result<TaxId> taxId = TaxId.Create(command.TaxId);
        if (taxId.IsFailure)
        {
            return taxId.Error;
        }

        if (await uniqueness.IsTaxIdTakenAsync(taxId.Value, cancellationToken))
        {
            return CustomerErrors.TaxIdAlreadyRegistered;
        }

        Result<Customer> customer = BuildCustomer(command, taxId.Value);
        if (customer.IsFailure)
        {
            return customer.Error;
        }

        customers.Add(customer.Value);
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return customer.Value.Id.Value;
    }

    private Result<Customer> BuildCustomer(RegisterCustomerCommand command, TaxId taxId)
    {
        Result<PersonName> name = PersonName.Create(command.Name);
        if (name.IsFailure)
        {
            return name.Error;
        }

        Result<EmailAddress> email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error;
        }

        Result<PhoneNumber?> phone = ParsePhone(command.Phone);
        if (phone.IsFailure)
        {
            return phone.Error;
        }

        return Customer.Register(name.Value, taxId, email.Value, phone.Value, clock.UtcNow);
    }

    private static Result<PhoneNumber?> ParsePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Result<PhoneNumber?>.Success(null);
        }

        Result<PhoneNumber> parsed = PhoneNumber.Create(phone);
        if (parsed.IsFailure)
        {
            return parsed.Error;
        }

        return parsed.Value;
    }
}
