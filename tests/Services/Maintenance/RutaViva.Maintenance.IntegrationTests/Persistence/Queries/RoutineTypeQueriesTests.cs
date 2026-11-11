using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Queries;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Queries;

[Trait("Category", "Integration")]
public sealed class RoutineTypeQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const int SeededTypes = 3;
    private const int OilIntervalKilometers = 10000;
    private const int OilIntervalMonths = 6;
    private const decimal DefaultDueSoonPercent = 10m;

    [Fact]
    public async Task GetAllAsync_Should_ReturnSeededRoutineTypes_When_DatabaseIsNew()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        IReadOnlyList<RoutineTypeDto> all = await new RoutineTypeQueries(context).GetAllAsync(cancellationToken);

        all.Should().HaveCount(SeededTypes);
        RoutineTypeDto oil = all.Single(routineType => routineType.Code == RoutineTypeSeed.OilChangeCode);
        oil.Id.Should().Be(RoutineTypeSeed.OilChangeId);
        oil.DefaultIntervalKm.Should().Be(OilIntervalKilometers);
        oil.DefaultIntervalMonths.Should().Be(OilIntervalMonths);
        oil.DueSoonPercent.Should().Be(DefaultDueSoonPercent);
    }
}
