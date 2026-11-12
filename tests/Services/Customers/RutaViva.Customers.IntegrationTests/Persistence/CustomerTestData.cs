using System.Globalization;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.IntegrationTests.Persistence;

internal static class CustomerTestData
{
    private const string CustomerName = "Acme Transport";
    private const string CustomerPhone = "+573001234567";
    private const string OccurredAtText = "2026-11-12T09:00:00-05:00";

    public static DateTimeOffset OccurredAt => DateTimeOffset.Parse(OccurredAtText, CultureInfo.InvariantCulture);

    public static PersonName Name(string value) => PersonName.Create(value).Value;

    public static EmailAddress Email(string value) => EmailAddress.Create(value).Value;

    public static Customer CreateCustomer(string taxId, string email) =>
        Customer.Register(
            Name(CustomerName),
            TaxId.Create(taxId).Value,
            Email(email),
            PhoneNumber.Create(CustomerPhone).Value,
            OccurredAt);
}
