using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.UnitTests;

internal static class WorkshopTestFactory
{
    public const string WorkshopName = "Autofrenos Medellín";
    public const string TaxIdValue = "900100001";
    public const string EmailValue = "contacto@autofrenos.example";
    public const string PhoneValue = "6044440001";
    public const string AddressValue = "Cra 50 # 10-20";
    public const string CityValue = "Medellín";

    public static RegisterWorkshopCommand Command(
        string? name = WorkshopName,
        string? taxId = TaxIdValue,
        string? email = EmailValue,
        string? phone = PhoneValue,
        string? address = AddressValue,
        string? city = CityValue) =>
        new(name, taxId, email, phone, address, city);

    public static Workshop ValidWorkshop() =>
        Workshop.Register(
            WorkshopName,
            TaxId.Create(TaxIdValue).Value,
            EmailAddress.Create(EmailValue).Value,
            PhoneNumber.Create(PhoneValue).Value,
            PostalAddress.Create(AddressValue, CityValue).Value).Value;
}
