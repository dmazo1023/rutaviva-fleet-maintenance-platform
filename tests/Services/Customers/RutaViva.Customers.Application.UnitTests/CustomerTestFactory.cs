using System.Globalization;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.UnitTests;

internal static class CustomerTestFactory
{
    public const string CustomerName = "RutaViva S.A.S.";
    public const string TaxIdText = "900000001";
    public const string CustomerEmail = "flota@rutaviva.example";
    public const string PhoneText = "6045550001";
    public const string ApproverName = "Laura Gómez";
    public const string ApproverEmail = "laura_gomez@rutaviva.com";

    private const string OccurredAtText = "2026-11-01T08:00:00-05:00";

    public static DateTimeOffset OccurredAt() =>
        DateTimeOffset.Parse(OccurredAtText, CultureInfo.InvariantCulture);

    public static Customer ActiveCustomer() =>
        Customer.Register(
            PersonName.Create(CustomerName).Value,
            TaxId.Create(TaxIdText).Value,
            EmailAddress.Create(CustomerEmail).Value,
            PhoneNumber.Create(PhoneText).Value,
            OccurredAt());

    public static Customer CustomerWithApprover()
    {
        Customer customer = ActiveCustomer();
        customer.AddApprover(PersonName.Create(ApproverName).Value, EmailAddress.Create(ApproverEmail).Value, OccurredAt());
        return customer;
    }
}
