using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Workshops.Domain.Common;

public static class WorkshopErrors
{
    private const string NotFoundCode = "Workshop.NotFound";
    private const string TaxIdAlreadyRegisteredCode = "Workshop.TaxIdAlreadyRegistered";
    private const string NameRequiredCode = "Workshop.NameRequired";
    private const string NameTooLongCode = "Workshop.NameTooLong";
    private const string IdentifierEmptyCode = "WorkshopId.Empty";
    private const string TaxIdInvalidCode = "TaxId.Invalid";
    private const string EmailInvalidCode = "EmailAddress.Invalid";
    private const string PhoneInvalidCode = "PhoneNumber.Invalid";
    private const string AddressRequiredCode = "PostalAddress.AddressRequired";
    private const string AddressTooLongCode = "PostalAddress.AddressTooLong";
    private const string CityRequiredCode = "PostalAddress.CityRequired";
    private const string CityTooLongCode = "PostalAddress.CityTooLong";

    private const string NotFoundMessage = "The workshop was not found.";
    private const string TaxIdAlreadyRegisteredMessage = "A workshop with this tax id is already registered.";
    private const string NameRequiredMessage = "The workshop name is required.";
    private const string NameTooLongMessage = "The workshop name exceeds the maximum length.";
    private const string IdentifierEmptyMessage = "The workshop identifier cannot be an empty value.";
    private const string TaxIdInvalidMessage = "The tax id must contain only digits and have between 9 and 10 digits.";
    private const string EmailInvalidMessage = "The email address is not valid.";
    private const string PhoneInvalidMessage = "The phone number must contain between 7 and 15 digits with an optional leading plus sign.";
    private const string AddressRequiredMessage = "The address is required.";
    private const string AddressTooLongMessage = "The address exceeds the maximum length.";
    private const string CityRequiredMessage = "The city is required.";
    private const string CityTooLongMessage = "The city exceeds the maximum length.";

    public static readonly Error NotFound = new(NotFoundCode, NotFoundMessage, ErrorType.NotFound);

    public static readonly Error TaxIdAlreadyRegistered = new(TaxIdAlreadyRegisteredCode, TaxIdAlreadyRegisteredMessage, ErrorType.Conflict);

    public static readonly Error NameRequired = new(NameRequiredCode, NameRequiredMessage, ErrorType.Validation);

    public static readonly Error NameTooLong = new(NameTooLongCode, NameTooLongMessage, ErrorType.Validation);

    public static readonly Error IdentifierEmpty = new(IdentifierEmptyCode, IdentifierEmptyMessage, ErrorType.Validation);

    public static readonly Error TaxIdInvalid = new(TaxIdInvalidCode, TaxIdInvalidMessage, ErrorType.Validation);

    public static readonly Error EmailInvalid = new(EmailInvalidCode, EmailInvalidMessage, ErrorType.Validation);

    public static readonly Error PhoneInvalid = new(PhoneInvalidCode, PhoneInvalidMessage, ErrorType.Validation);

    public static readonly Error AddressRequired = new(AddressRequiredCode, AddressRequiredMessage, ErrorType.Validation);

    public static readonly Error AddressTooLong = new(AddressTooLongCode, AddressTooLongMessage, ErrorType.Validation);

    public static readonly Error CityRequired = new(CityRequiredCode, CityRequiredMessage, ErrorType.Validation);

    public static readonly Error CityTooLong = new(CityTooLongCode, CityTooLongMessage, ErrorType.Validation);
}
