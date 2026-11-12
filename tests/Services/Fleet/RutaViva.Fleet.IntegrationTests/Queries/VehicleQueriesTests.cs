using RutaViva.Fleet.Application.Vehicles.Queries;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.Fleet.Infrastructure.Persistence;
using RutaViva.Fleet.Infrastructure.Queries;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Queries;

[Trait("Category", "Integration")]
public sealed class VehicleQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string CustomerName = "RutaViva S.A.S.";
    private const string OtherCustomerName = "Transportes Andinos S.A.";
    private const string WorkshopName = "Taller Central";
    private const string ApproverName = "Ana Gomez";
    private const string KangooModel = "Kangoo";
    private const string MasterModel = "Master";
    private const string LowercaseKangoo = "kangoo";
    private const string NormalizedPlateText = "RVA101";
    private const string LowercasePlateText = "rva101";
    private const string MissingPlateText = "ZZZ999";
    private const int FirstPage = 1;
    private const int SecondPage = 2;
    private const int LargePageSize = 50;
    private const int SmallPageSize = 1;
    private const int TwoVehicles = 2;
    private const int OneVehicle = 1;

    [Fact]
    public async Task ListAsync_Should_ReturnOnlyActiveVehiclesOfCustomerWithTotal_When_CustomerAndStatusAreProvided()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        VehicleQuerySeeder seeder = new(context);
        CustomerReference customer = seeder.AddCustomer(CustomerName);
        CustomerReference otherCustomer = seeder.AddCustomer(OtherCustomerName);
        WorkshopReference workshop = seeder.AddWorkshop(WorkshopName);
        seeder.AddVehicle(customer, workshop, null, KangooModel);
        seeder.AddVehicle(customer, workshop, null, MasterModel);
        Vehicle inactive = seeder.AddVehicle(customer, workshop, null, MasterModel);
        seeder.AddVehicle(otherCustomer, workshop, null, MasterModel);
        inactive.Deactivate(DateTimeOffset.UtcNow);
        await seeder.SaveAsync(cancellationToken);
        VehicleQueries queries = new(context);

        PagedResult<VehicleSummaryDto> page = await queries.ListAsync(
            new VehicleListCriteria(customer.Id, VehicleStatus.Active, null, FirstPage, LargePageSize),
            cancellationToken);

        page.TotalCount.Should().Be(TwoVehicles);
        page.Items.Should().HaveCount(TwoVehicles);
        page.Items.Should().OnlyContain(item => item.Status == VehicleStatus.Active && item.CustomerId == customer.Id);
        page.Items.Should().OnlyContain(item => item.OwnerName == CustomerName && item.WorkshopName == WorkshopName);
    }

    [Fact]
    public async Task ListAsync_Should_ReturnRequestedPageWithFullTotal_When_PageSizeIsSmallerThanTotal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        VehicleQuerySeeder seeder = new(context);
        CustomerReference customer = seeder.AddCustomer(CustomerName);
        WorkshopReference workshop = seeder.AddWorkshop(WorkshopName);
        seeder.AddVehicle(customer, workshop, null, KangooModel);
        seeder.AddVehicle(customer, workshop, null, MasterModel);
        await seeder.SaveAsync(cancellationToken);
        VehicleQueries queries = new(context);

        PagedResult<VehicleSummaryDto> second = await queries.ListAsync(
            new VehicleListCriteria(customer.Id, null, null, SecondPage, SmallPageSize),
            cancellationToken);

        second.TotalCount.Should().Be(TwoVehicles);
        second.Page.Should().Be(SecondPage);
        second.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task ListAsync_Should_FindVehiclesIgnoringCase_When_SearchIsLowercaseModel()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        VehicleQuerySeeder seeder = new(context);
        CustomerReference customer = seeder.AddCustomer(CustomerName);
        WorkshopReference workshop = seeder.AddWorkshop(WorkshopName);
        seeder.AddVehicle(customer, workshop, null, KangooModel);
        seeder.AddVehicle(customer, workshop, null, MasterModel);
        await seeder.SaveAsync(cancellationToken);
        VehicleQueries queries = new(context);

        PagedResult<VehicleSummaryDto> page = await queries.ListAsync(
            new VehicleListCriteria(customer.Id, null, LowercaseKangoo, FirstPage, LargePageSize),
            cancellationToken);

        page.TotalCount.Should().Be(OneVehicle);
        page.Items.Single().Model.Should().Be(KangooModel);
    }

    [Fact]
    public async Task GetByPlateAsync_Should_ReturnDetailWithReplicaNames_When_PlateIsNormalized()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        VehicleQuerySeeder seeder = new(context);
        CustomerReference customer = seeder.AddCustomer(CustomerName);
        WorkshopReference workshop = seeder.AddWorkshop(WorkshopName);
        ApproverReference approver = seeder.AddApprover(customer, ApproverName);
        seeder.AddVehicle(customer, workshop, approver, KangooModel, NormalizedPlateText);
        await seeder.SaveAsync(cancellationToken);
        VehicleQueries queries = new(context);
        Plate plate = Plate.Create(LowercasePlateText).Value;

        VehicleDetailDto? detail = await queries.GetByPlateAsync(plate, cancellationToken);

        detail.Should().NotBeNull();
        detail.Plate.Should().Be(NormalizedPlateText);
        detail.Owner.Name.Should().Be(CustomerName);
        detail.Owner.TaxId.Should().Be(customer.TaxId);
        detail.Workshop.Name.Should().Be(WorkshopName);
        detail.Responsible.Should().NotBeNull();
        detail.Responsible.Name.Should().Be(ApproverName);
    }

    [Fact]
    public async Task GetDetailAsync_Should_ReturnVehicleWithoutResponsible_When_NoApproverIsAssigned()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        VehicleQuerySeeder seeder = new(context);
        CustomerReference customer = seeder.AddCustomer(CustomerName);
        WorkshopReference workshop = seeder.AddWorkshop(WorkshopName);
        Vehicle vehicle = seeder.AddVehicle(customer, workshop, null, MasterModel);
        await seeder.SaveAsync(cancellationToken);
        VehicleQueries queries = new(context);

        VehicleDetailDto? detail = await queries.GetDetailAsync(vehicle.Id, cancellationToken);

        detail.Should().NotBeNull();
        detail.Id.Should().Be(vehicle.Id);
        detail.Responsible.Should().BeNull();
        detail.Status.Should().Be(VehicleStatus.Active);
    }

    [Fact]
    public async Task GetByPlateAsync_Should_ReturnNull_When_PlateDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        VehicleQueries queries = new(context);
        Plate plate = Plate.Create(MissingPlateText).Value;

        VehicleDetailDto? detail = await queries.GetByPlateAsync(plate, cancellationToken);

        detail.Should().BeNull();
    }
}
