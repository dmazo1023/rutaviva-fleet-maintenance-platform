using System.Globalization;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Repositories;

[Trait("Category", "Integration")]
public sealed class MaintenanceUnitOfWorkTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ConflictPlate = "CNF123";
    private const string SuccessPlate = "SCS123";
    private const string NewReadAtText = "2026-11-13T08:00:00-05:00";
    private const long AdvancedKilometers = 53000L;

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnConflictAndClearTracker_When_PlanWasModifiedByAnotherContext()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan seeded = await PlanSeeding.SavePlanAsync(database, ConflictPlate, cancellationToken);
        await using MaintenanceDbContext firstContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        await using MaintenanceDbContext secondContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        MaintenancePlan firstPlan = (await new MaintenancePlanRepository(firstContext).GetAsync(seeded.Id, cancellationToken))!;
        MaintenancePlan secondPlan = (await new MaintenancePlanRepository(secondContext).GetAsync(seeded.Id, cancellationToken))!;
        RoutineParameterCatalog catalog = await new RoutineTypeRepository(secondContext).GetCatalogAsync(cancellationToken);
        firstPlan.Suspend();
        Result firstResult = await new MaintenanceUnitOfWork(firstContext).SaveChangesAsync(cancellationToken);
        secondPlan.RecordOdometerReading(CreateReading(), catalog);

        Result secondResult = await new MaintenanceUnitOfWork(secondContext).SaveChangesAsync(cancellationToken);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be(ConcurrencyErrors.Conflict.Code);
        secondContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_KeepTrackedEntities_When_SaveSucceeds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan seeded = await PlanSeeding.SavePlanAsync(database, SuccessPlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        MaintenancePlan plan = (await new MaintenancePlanRepository(context).GetAsync(seeded.Id, cancellationToken))!;
        plan.Suspend();

        Result result = await new MaintenanceUnitOfWork(context).SaveChangesAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Entries().Should().NotBeEmpty();
    }

    private static OdometerReading CreateReading()
    {
        DateTimeOffset readAt = DateTimeOffset.Parse(NewReadAtText, CultureInfo.InvariantCulture);
        return OdometerReading.Create(AdvancedKilometers, readAt).Value;
    }
}
