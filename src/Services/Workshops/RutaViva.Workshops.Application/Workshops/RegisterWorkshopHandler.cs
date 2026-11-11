using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.Workshops;

public sealed class RegisterWorkshopHandler(
    IWorkshopRepository workshops,
    IWorkshopUniquenessChecker uniqueness,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterWorkshopCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterWorkshopCommand command, CancellationToken cancellationToken)
    {
        Result<Workshop> workshop = BuildWorkshop(command);
        if (workshop.IsFailure)
        {
            return workshop.Error;
        }

        bool taxIdTaken = await uniqueness.IsTaxIdTakenAsync(workshop.Value.TaxId, cancellationToken);
        if (taxIdTaken)
        {
            return WorkshopErrors.TaxIdAlreadyRegistered;
        }

        workshops.Add(workshop.Value);
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return workshop.Value.Id.Value;
    }

    private static Result<Workshop> BuildWorkshop(RegisterWorkshopCommand command)
    {
        Result<TaxId> taxId = TaxId.Create(command.TaxId);
        if (taxId.IsFailure)
        {
            return taxId.Error;
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

        Result<PostalAddress> address = PostalAddress.Create(command.Address, command.City);
        if (address.IsFailure)
        {
            return address.Error;
        }

        return Workshop.Register(command.Name, taxId.Value, email.Value, phone.Value, address.Value);
    }

    private static Result<PhoneNumber?> ParsePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<PhoneNumber?>.Success(null);
        }

        Result<PhoneNumber> phone = PhoneNumber.Create(value);
        if (phone.IsFailure)
        {
            return Result<PhoneNumber?>.Failure(phone.Error);
        }

        return Result<PhoneNumber?>.Success(phone.Value);
    }
}
