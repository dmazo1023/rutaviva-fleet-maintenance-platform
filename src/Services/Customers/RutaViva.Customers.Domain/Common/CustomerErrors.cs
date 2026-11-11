using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Customers.Domain.Common;

public static class CustomerErrors
{
    private const string TaxIdAlreadyRegisteredCode = "Customer.TaxIdAlreadyRegistered";
    private const string ApproverEmailAlreadyRegisteredCode = "Customer.ApproverEmailAlreadyRegistered";
    private const string NotFoundCode = "Customer.NotFound";
    private const string ApproverNotFoundCode = "Customer.ApproverNotFound";
    private const string InactiveCode = "Customer.Inactive";
    private const string IdentifierEmptyCode = "Identifier.Empty";
    private const string TaxIdInvalidCode = "TaxId.Invalid";
    private const string EmailAddressInvalidCode = "EmailAddress.Invalid";
    private const string EmailAddressTooLongCode = "EmailAddress.TooLong";
    private const string PhoneNumberInvalidCode = "PhoneNumber.Invalid";
    private const string PersonNameRequiredCode = "PersonName.Required";
    private const string PersonNameTooLongCode = "PersonName.TooLong";

    private const string TaxIdAlreadyRegisteredMessage = "A customer with this tax id is already registered.";
    private const string ApproverEmailAlreadyRegisteredMessage = "An approver with this email is already registered for the customer.";
    private const string NotFoundMessage = "The customer was not found.";
    private const string ApproverNotFoundMessage = "The approver was not found in the customer.";
    private const string InactiveMessage = "The customer is inactive.";
    private const string IdentifierEmptyMessage = "The identifier cannot be an empty value.";
    private const string TaxIdInvalidMessage = "The tax id must contain digits with an optional verification digit after a hyphen.";
    private const string EmailAddressInvalidMessage = "The email address does not have a valid format.";
    private const string EmailAddressTooLongMessage = "The email address exceeds the maximum length.";
    private const string PhoneNumberInvalidMessage = "The phone number must contain digits with an optional leading plus sign.";
    private const string PersonNameRequiredMessage = "The name is required.";
    private const string PersonNameTooLongMessage = "The name exceeds the maximum length.";

    public static readonly Error TaxIdAlreadyRegistered = new(TaxIdAlreadyRegisteredCode, TaxIdAlreadyRegisteredMessage, ErrorType.Conflict);

    public static readonly Error ApproverEmailAlreadyRegistered = new(ApproverEmailAlreadyRegisteredCode, ApproverEmailAlreadyRegisteredMessage, ErrorType.Conflict);

    public static readonly Error NotFound = new(NotFoundCode, NotFoundMessage, ErrorType.NotFound);

    public static readonly Error ApproverNotFound = new(ApproverNotFoundCode, ApproverNotFoundMessage, ErrorType.NotFound);

    public static readonly Error Inactive = new(InactiveCode, InactiveMessage, ErrorType.BusinessRule);

    public static readonly Error IdentifierEmpty = new(IdentifierEmptyCode, IdentifierEmptyMessage, ErrorType.Validation);

    public static readonly Error TaxIdInvalid = new(TaxIdInvalidCode, TaxIdInvalidMessage, ErrorType.Validation);

    public static readonly Error EmailAddressInvalid = new(EmailAddressInvalidCode, EmailAddressInvalidMessage, ErrorType.Validation);

    public static readonly Error EmailAddressTooLong = new(EmailAddressTooLongCode, EmailAddressTooLongMessage, ErrorType.Validation);

    public static readonly Error PhoneNumberInvalid = new(PhoneNumberInvalidCode, PhoneNumberInvalidMessage, ErrorType.Validation);

    public static readonly Error PersonNameRequired = new(PersonNameRequiredCode, PersonNameRequiredMessage, ErrorType.Validation);

    public static readonly Error PersonNameTooLong = new(PersonNameTooLongCode, PersonNameTooLongMessage, ErrorType.Validation);
}
