using System.Globalization;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Infrastructure.Persistence;
using RutaViva.Fleet.Infrastructure.Persistence.Repositories;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Persistence;

internal static class FleetTestData
{
    public const string ActiveStatus = "Active";
    public const string InactiveStatus = "Inactive";
    public const string BrandText = "Renault";
    public const string ModelText = "Kangoo";
    public const string NameText = "Transportes Andinos S.A.";
    public const string EmailText = "approver@rutaviva.test";
    public const string CityText = "Bogota";
    public const int ModelYear = 2022;
    public const long OdometerKm = 52300;

    private const string TaxIdFormat = "9{0:D8}";
    private const string MetadataType = "Test";
    private const string MetadataSubject = "test.subject";
    private const long MetadataOffset = 1;
    private const int Year = 2026;
    private const int Month = 11;
    private const int Day = 12;
    private const int LaterHours = 2;

    private static int _taxIdCounter;

    public static DateTimeOffset Instant() => new(Year, Month, Day, 0, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset LaterInstant() => Instant().AddHours(LaterHours);

    public static EventMetadata Metadata() => new(Guid.NewGuid(), MetadataOffset, MetadataType, MetadataSubject, Instant());

    public static string NextTaxId() =>
        string.Format(CultureInfo.InvariantCulture, TaxIdFormat, Interlocked.Increment(ref _taxIdCounter));

    public static async Task<(CustomerId Customer, WorkshopId Workshop)> SeedPartiesAsync(
        SqlServerTestDatabase database,
        CancellationToken cancellationToken)
    {
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        CustomerReference customer = CustomerReference.Create(CustomerId.New(), NextTaxId(), NameText, ReferenceStatus.Active, Instant());
        WorkshopReference workshop = WorkshopReference.Create(WorkshopId.New(), NextTaxId(), NameText, ReferenceStatus.Active, Instant());
        context.CustomerReferences.Add(customer);
        context.WorkshopReferences.Add(workshop);
        await context.SaveChangesAsync(cancellationToken);
        return (customer.Id, workshop.Id);
    }

    public static RegisterVehicleCommand Command(CustomerId customerId, WorkshopId workshopId, string plate, string? vin) =>
        new(plate, vin, BrandText, ModelText, ModelYear, OdometerKm, customerId, workshopId, null);

    public static async Task<Result<Guid>> RegisterAsync(
        SqlServerTestDatabase database,
        RegisterVehicleCommand command,
        CancellationToken cancellationToken)
    {
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        RegisterVehicleHandler handler = new(
            new VehicleRepository(context),
            new PassingUniquenessChecker(),
            new FleetReferenceStore(context),
            new FleetUnitOfWork(context),
            new FakeClock(Instant()));
        return await handler.HandleAsync(command, cancellationToken);
    }

    public static async Task<T> InContextAsync<T>(
        SqlServerTestDatabase database,
        Func<FleetDbContext, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        return await action(context);
    }

    public static IUnitOfWork UnitOfWorkOf(FleetDbContext context) => new FleetUnitOfWork(context);

    public static IFleetReferenceReader ReaderOf(FleetDbContext context) => new FleetReferenceStore(context);

    public static IFleetReferenceWriter WriterOf(FleetDbContext context) => new FleetReferenceStore(context);
}
