using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.UnitTests.Vehicles;

internal static class VehicleTestData
{
    public const string PlateText = "RVA101";
    public const string VinText = "9FB4SRC9AKM100101";
    public const string BrandText = "Renault";
    public const string ModelText = "Kangoo";
    public const string TaxIdText = "900000001";
    public const string NameText = "Transportes Andinos S.A.";
    public const string EmailText = "approver@rutaviva.test";
    public const int ModelYear = 2022;
    public const int CurrentYear = 2026;
    public const long OdometerKm = 52300;
    private const int NowMonth = 11;
    private const int NowDay = 12;

    public static readonly DateTimeOffset Now = new(CurrentYear, NowMonth, NowDay, 0, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset Later = Now.AddHours(1);

    public static Plate NewPlate() => Plate.Create(PlateText).Value;

    public static Vin NewVin() => Vin.Create(VinText).Value;

    public static VehicleDescription NewDescription() =>
        VehicleDescription.Create(BrandText, ModelText, ModelYear, CurrentYear).Value;

    public static Kilometers NewOdometer() => Kilometers.Create(OdometerKm).Value;

    public static CustomerReference Owner(ReferenceStatus status = ReferenceStatus.Active) =>
        CustomerReference.Create(CustomerId.New(), TaxIdText, NameText, status, Now);

    public static WorkshopReference Workshop(ReferenceStatus status = ReferenceStatus.Active) =>
        WorkshopReference.Create(WorkshopId.New(), TaxIdText, NameText, status, Now);

    public static ApproverReference Approver(CustomerId customerId, ReferenceStatus status = ReferenceStatus.Active) =>
        ApproverReference.Create(ApproverId.New(), customerId, NameText, EmailText, status, Now);
}
