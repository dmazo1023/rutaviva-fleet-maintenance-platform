using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Repositories;

[Trait("Category", "Integration")]
public sealed class AlertRepositoryTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string DuplicatePlate = "DUP123";
    private const string OpenPlate = "OPN123";
    private const string RaisedAtText = "2026-11-12T09:00:00-05:00";
    private const string AcknowledgedBy = "operator";

    [Fact]
    public async Task SaveChanges_Should_RejectDuplicate_When_AlertRepeatsRoutineCycleAndType()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan plan = await PlanSeeding.SavePlanAsync(database, DuplicatePlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        AlertRepository alerts = new(context);
        MaintenanceUnitOfWork unitOfWork = new(context);
        alerts.Add(RaiseAlert(plan, AlertType.DueSoon));
        Result first = await unitOfWork.SaveChangesAsync(cancellationToken);
        alerts.Add(RaiseAlert(plan, AlertType.DueSoon));

        Func<Task> saveDuplicate = async () => await unitOfWork.SaveChangesAsync(cancellationToken);

        first.IsSuccess.Should().BeTrue();
        await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetOpenByRoutineAsync_Should_ReturnOnlyOpenAlerts_When_SomeAreResolved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan plan = await PlanSeeding.SavePlanAsync(database, OpenPlate, cancellationToken);
        await using MaintenanceDbContext writeContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        Alert dueSoon = RaiseAlert(plan, AlertType.DueSoon);
        Alert overdue = RaiseAlert(plan, AlertType.Overdue);
        new AlertRepository(writeContext).Add(dueSoon);
        new AlertRepository(writeContext).Add(overdue);
        dueSoon.Acknowledge(AcknowledgedBy, ParseRaisedAt()).IsSuccess.Should().BeTrue();
        overdue.Resolve(AlertResolutionReason.RoutineRestarted, ParseRaisedAt()).IsSuccess.Should().BeTrue();
        await writeContext.SaveChangesAsync(cancellationToken);
        await using MaintenanceDbContext readContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        IReadOnlyList<Alert> open = await new AlertRepository(readContext)
            .GetOpenByRoutineAsync(plan.Routines.Single().Id, null, cancellationToken);
        IReadOnlyList<Alert> openOverdue = await new AlertRepository(readContext)
            .GetOpenByRoutineAsync(plan.Routines.Single().Id, AlertType.Overdue, cancellationToken);

        open.Should().ContainSingle().Which.Id.Should().Be(dueSoon.Id);
        openOverdue.Should().BeEmpty();
    }

    private static DateTimeOffset ParseRaisedAt() =>
        DateTimeOffset.Parse(RaisedAtText, CultureInfo.InvariantCulture);

    private static Alert RaiseAlert(MaintenancePlan plan, AlertType type)
    {
        MaintenanceRoutine routine = plan.Routines.Single();
        AlertOrigin origin = new(plan.Id, plan.Plate, plan.CustomerId, routine.Id, routine.Name, routine.Cycle.Number);
        return Alert.Raise(origin, type, AlertTrigger.Distance, ParseRaisedAt());
    }
}
