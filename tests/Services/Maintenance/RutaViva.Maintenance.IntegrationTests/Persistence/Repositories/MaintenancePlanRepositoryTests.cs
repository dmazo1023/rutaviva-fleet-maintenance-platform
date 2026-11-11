using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Repositories;

[Trait("Category", "Integration")]
public sealed class MaintenancePlanRepositoryTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string FirstPlate = "AAA111";
    private const string SecondPlate = "BBB222";
    private const string ThirdPlate = "CCC333";
    private const string LookupPlate = "DDD444";
    private const string PagedFirstPlate = "EEE555";
    private const string PagedSecondPlate = "FFF666";
    private const string PagedThirdPlate = "GGG777";
    private const string UnknownPlate = "ZZZ999";
    private const int PageSize = 2;
    private const int ExpectedMatches = 2;
    private const int PagedPlanCount = 3;

    [Fact]
    public async Task GetByPlatesAsync_Should_ReturnPlansByPlate_When_PlatesExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan first = await PlanSeeding.SavePlanAsync(database, FirstPlate, cancellationToken);
        MaintenancePlan second = await PlanSeeding.SavePlanAsync(database, SecondPlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        List<LicensePlate> requested = [first.Plate, second.Plate, LicensePlate.Create(UnknownPlate).Value];

        IReadOnlyDictionary<LicensePlate, MaintenancePlan> plans = await new MaintenancePlanRepository(context)
            .GetByPlatesAsync(requested, cancellationToken);

        plans.Should().HaveCount(ExpectedMatches);
        plans[first.Plate].Id.Should().Be(first.Id);
        plans[second.Plate].Id.Should().Be(second.Id);
        plans[first.Plate].Routines.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByPlatesAsync_Should_ReturnEmpty_When_NoPlatesAreRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        IReadOnlyDictionary<LicensePlate, MaintenancePlan> plans = await new MaintenancePlanRepository(context)
            .GetByPlatesAsync([], cancellationToken);

        plans.Should().BeEmpty();
    }

    [Fact]
    public async Task FindVehicleIdByPlateAsync_Should_ReturnVehicleId_When_PlateExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan seeded = await PlanSeeding.SavePlanAsync(database, LookupPlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        MaintenancePlanRepository repository = new(context);

        VehicleId? found = await repository.FindVehicleIdByPlateAsync(seeded.Plate, cancellationToken);
        VehicleId? missing = await repository.FindVehicleIdByPlateAsync(LicensePlate.Create(UnknownPlate).Value, cancellationToken);

        found.Should().Be(seeded.Id);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task GetActivePlanIdsPageAsync_Should_PageInVehicleIdOrder_When_PlansExceedPageSize()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan pagedFirst = await PlanSeeding.SavePlanAsync(database, PagedFirstPlate, cancellationToken);
        MaintenancePlan pagedSecond = await PlanSeeding.SavePlanAsync(database, PagedSecondPlate, cancellationToken);
        MaintenancePlan pagedThird = await PlanSeeding.SavePlanAsync(database, PagedThirdPlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        MaintenancePlanRepository repository = new(context);

        IReadOnlyList<VehicleId> collected = await CollectPagesAsync(repository, cancellationToken);

        collected.Should().HaveCountGreaterThanOrEqualTo(PagedPlanCount);
        collected.Should().Contain([pagedFirst.Id, pagedSecond.Id, pagedThird.Id]);
        collected.Should().OnlyHaveUniqueItems();
        collected.Should().Equal(await ReadOrderedIdsAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<VehicleId>> CollectPagesAsync(
        MaintenancePlanRepository repository,
        CancellationToken cancellationToken)
    {
        List<VehicleId> collected = [];
        VehicleId? after = null;
        while (true)
        {
            IReadOnlyList<VehicleId> page = await repository.GetActivePlanIdsPageAsync(after, PageSize, cancellationToken);
            page.Count.Should().BeLessThanOrEqualTo(PageSize);
            if (page.Count == 0)
            {
                return collected;
            }

            collected.AddRange(page);
            after = page[^1];
        }
    }

    private async Task<IReadOnlyList<VehicleId>> ReadOrderedIdsAsync(CancellationToken cancellationToken)
    {
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        return await new MaintenancePlanRepository(context).GetActivePlanIdsPageAsync(null, int.MaxValue, cancellationToken);
    }
}
