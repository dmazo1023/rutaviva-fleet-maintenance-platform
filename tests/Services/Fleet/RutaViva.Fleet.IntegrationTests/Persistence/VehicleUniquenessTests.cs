using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.Fleet.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class VehicleUniquenessTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string RacePlate = "RAC101";
    private const string RaceFirstVin = "9FB4SRC9AKM200101";
    private const string RaceSecondVin = "9FB4SRC9AKM200102";
    private const string VinRacePlateFirst = "VIN101";
    private const string VinRacePlateSecond = "VIN102";
    private const string VinRaceVin = "9FB4SRC9AKM300101";
    private const string NoVinPlateFirst = "NVN101";
    private const string NoVinPlateSecond = "NVN102";
    private const string CheckerPlate = "CHK101";
    private const string CheckerVin = "9FB4SRC9AKM400101";
    private const string UnknownPlate = "UNK101";
    private const string UnknownVin = "9FB4SRC9AKM500101";
    private const string VinCheckerPlate = "VCK101";
    private const int ExpectedNoRows = 0;
    private const int ExpectedSingleRow = 1;
    private const int ExpectedTwoRows = 2;

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnPlateAlreadyRegistered_When_SamePlateIsSavedAfterCheckerPassed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);

        Result<Guid> first = await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, RacePlate, RaceFirstVin), cancellationToken);
        Result<Guid> second = await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, RacePlate, RaceSecondVin), cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(VehicleErrors.PlateAlreadyRegistered.Code);
        (await CountByPlateAsync(RacePlate, cancellationToken)).Should().Be(ExpectedSingleRow);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnVinAlreadyRegistered_When_SameVinIsSavedAfterCheckerPassed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);

        Result<Guid> first = await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, VinRacePlateFirst, VinRaceVin), cancellationToken);
        Result<Guid> second = await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, VinRacePlateSecond, VinRaceVin), cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(VehicleErrors.VinAlreadyRegistered.Code);
        (await CountByPlateAsync(VinRacePlateSecond, cancellationToken)).Should().Be(ExpectedNoRows);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_AcceptSeveralVehicles_When_VinIsMissing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);

        Result<Guid> first = await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, NoVinPlateFirst, null), cancellationToken);
        Result<Guid> second = await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, NoVinPlateSecond, null), cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        (await CountByCustomerAsync(customer, cancellationToken)).Should().Be(ExpectedTwoRows);
    }

    [Fact]
    public async Task IsPlateTakenAsync_Should_ReturnTrueOnlyForStoredPlate_When_VehicleWasRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, CheckerPlate, CheckerVin), cancellationToken);

        bool stored = await IsPlateTakenAsync(CheckerPlate, cancellationToken);
        bool unknown = await IsPlateTakenAsync(UnknownPlate, cancellationToken);

        stored.Should().BeTrue();
        unknown.Should().BeFalse();
    }

    [Fact]
    public async Task IsVinTakenAsync_Should_ReturnTrueOnlyForStoredVin_When_VehicleWasRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await FleetTestData.RegisterAsync(database, FleetTestData.Command(customer, workshop, VinCheckerPlate, UnknownVin), cancellationToken);

        bool stored = await IsVinTakenAsync(UnknownVin, cancellationToken);
        bool unknown = await IsVinTakenAsync(CheckerVin, cancellationToken);

        stored.Should().BeTrue();
        unknown.Should().BeFalse();
    }

    private Task<int> CountByPlateAsync(string plate, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => context.Vehicles.AsNoTracking().CountAsync(vehicle => vehicle.Plate == Plate.Create(plate).Value, cancellationToken),
            cancellationToken);

    private Task<int> CountByCustomerAsync(CustomerId customer, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => context.Vehicles.AsNoTracking().CountAsync(vehicle => vehicle.CustomerId == customer, cancellationToken),
            cancellationToken);

    private Task<bool> IsPlateTakenAsync(string plate, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new VehicleUniquenessChecker(context).IsPlateTakenAsync(Plate.Create(plate).Value, cancellationToken),
            cancellationToken);

    private Task<bool> IsVinTakenAsync(string vin, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new VehicleUniquenessChecker(context).IsVinTakenAsync(Vin.Create(vin).Value, cancellationToken),
            cancellationToken);
}
