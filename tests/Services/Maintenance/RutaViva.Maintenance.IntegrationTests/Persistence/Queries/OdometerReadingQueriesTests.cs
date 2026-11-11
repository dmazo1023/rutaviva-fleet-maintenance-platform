using System.Globalization;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Queries;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Queries;

[Trait("Category", "Integration")]
public sealed class OdometerReadingQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string Plate = "RVA101";
    private const string ServiceDate = "2026-09-01";
    private const string OldestReadAtText = "2026-11-10T08:00:00-05:00";
    private const string MiddleReadAtText = "2026-11-11T08:00:00-05:00";
    private const string NewestReadAtText = "2026-11-12T08:00:00-05:00";
    private const long Odometer = 52300L;
    private const long ServiceKilometers = 45000L;
    private const long OldestKilometers = 51000L;
    private const long MiddleKilometers = 52000L;
    private const long NewestKilometers = 53000L;
    private const int Limit = 2;

    [Fact]
    public async Task GetRecentAsync_Should_ReturnLatestReadingsByReadAtDescending_When_LimitIsSmallerThanTotal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan plan = await QueryPlanSeeder.SaveAsync(
            database,
            Plate,
            CustomerId.New(),
            Odometer,
            [new RoutineSeed(RoutineTypeSeed.OilChangeId, ServiceKilometers, ServiceDate, true)],
            cancellationToken);
        await using MaintenanceDbContext writeContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        writeContext.OdometerReadings.AddRange(
            Record(plan.Id, MiddleKilometers, MiddleReadAtText),
            Record(plan.Id, OldestKilometers, OldestReadAtText),
            Record(plan.Id, NewestKilometers, NewestReadAtText));
        await writeContext.SaveChangesAsync(cancellationToken);
        await using MaintenanceDbContext readContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        IReadOnlyList<OdometerReadingDto> recent = await new OdometerReadingQueries(readContext).GetRecentAsync(plan.Id, Limit, cancellationToken);

        recent.Select(reading => reading.Kilometers).Should().Equal(NewestKilometers, MiddleKilometers);
    }

    private static OdometerReadingRecord Record(VehicleId vehicleId, long kilometers, string readAtText)
    {
        DateTimeOffset readAt = DateTimeOffset.Parse(readAtText, CultureInfo.InvariantCulture);
        return new OdometerReadingRecord
        {
            VehicleId = vehicleId,
            Kilometers = kilometers,
            ReadAt = readAt,
            ReceivedAt = readAt,
            Outcome = OdometerReadingOutcome.Accepted
        };
    }
}
