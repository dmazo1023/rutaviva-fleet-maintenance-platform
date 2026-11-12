using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.Fleet.Infrastructure.Persistence;

namespace RutaViva.Fleet.IntegrationTests.Queries;

internal sealed class VehicleQuerySeeder(FleetDbContext context)
{
    private const string PlatePrefix = "QRY";
    private const string VinPrefix = "9FB4SRC9AKM";
    private const string PlateFormat = "D3";
    private const string VinFormat = "D6";
    private const string TaxIdPrefix = "NIT";
    private const string TaxIdFormat = "D9";
    private const string DefaultBrand = "Renault";
    private const string ApproverEmail = "approver@rutaviva.test";
    private const int ModelYear = 2022;
    private const int CurrentYear = 2026;
    private const long OdometerKm = 52300;
    private const int RegistrationYear = 2026;
    private const int RegistrationMonth = 11;
    private const int RegistrationDay = 12;

    private static int _sequence;

    private static readonly DateTimeOffset BaseInstant = new(RegistrationYear, RegistrationMonth, RegistrationDay, 8, 0, 0, TimeSpan.Zero);

    public CustomerReference AddCustomer(string name)
    {
        CustomerReference customer = CustomerReference.Create(CustomerId.New(), NextTaxId(), name, ReferenceStatus.Active, BaseInstant);
        context.CustomerReferences.Add(customer);
        return customer;
    }

    public WorkshopReference AddWorkshop(string name)
    {
        WorkshopReference workshop = WorkshopReference.Create(WorkshopId.New(), NextTaxId(), name, ReferenceStatus.Active, BaseInstant);
        context.WorkshopReferences.Add(workshop);
        return workshop;
    }

    public ApproverReference AddApprover(CustomerReference customer, string fullName)
    {
        ApproverReference approver = ApproverReference.Create(ApproverId.New(), customer.Id, fullName, ApproverEmail, ReferenceStatus.Active, BaseInstant);
        context.ApproverReferences.Add(approver);
        return approver;
    }

    public Vehicle AddVehicle(
        CustomerReference owner,
        WorkshopReference workshop,
        ApproverReference? responsible,
        string model,
        string? plate = null)
    {
        int sequence = Interlocked.Increment(ref _sequence);
        Plate vehiclePlate = Plate.Create(plate ?? PlatePrefix + sequence.ToString(PlateFormat)).Value;
        Vin vin = Vin.Create(VinPrefix + sequence.ToString(VinFormat)).Value;
        VehicleDescription description = VehicleDescription.Create(DefaultBrand, model, ModelYear, CurrentYear).Value;
        Kilometers odometer = Kilometers.Create(OdometerKm).Value;
        Vehicle vehicle = Vehicle.Register(vehiclePlate, vin, description, odometer, owner, workshop, responsible, BaseInstant.AddMinutes(sequence)).Value;
        context.Vehicles.Add(vehicle);
        return vehicle;
    }

    private static string NextTaxId() => TaxIdPrefix + Interlocked.Increment(ref _sequence).ToString(TaxIdFormat);

    public Task SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
