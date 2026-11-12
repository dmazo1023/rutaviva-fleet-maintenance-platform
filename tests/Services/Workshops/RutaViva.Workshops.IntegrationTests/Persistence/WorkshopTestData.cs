using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.IntegrationTests.Persistence;

internal static class WorkshopTestData
{
    public const string DefaultName = "Autofrenos Medellin";
    public const string DefaultEmail = "contacto@autofrenos.example";
    public const string DefaultPhone = "6044440001";
    public const string DefaultAddress = "Cra 50 # 10-20";

    public static Workshop Create(string taxId, string city, string name = DefaultName, string? phone = DefaultPhone)
    {
        TaxId parsedTaxId = TaxId.Create(taxId).Value;
        EmailAddress email = EmailAddress.Create(DefaultEmail).Value;
        PhoneNumber? parsedPhone = ParsePhone(phone);
        PostalAddress address = PostalAddress.Create(DefaultAddress, city).Value;
        return Workshop.Register(name, parsedTaxId, email, parsedPhone, address).Value;
    }

    private static PhoneNumber? ParsePhone(string? phone)
    {
        if (phone is null)
        {
            return null;
        }

        return PhoneNumber.Create(phone).Value;
    }
}
