using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class SchemaTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string PlansUniquePlateIndex = "UX_MaintenancePlans_Plate";
    private const string RoutinesUniqueTypeIndex = "UX_MaintenanceRoutines_VehicleId_RoutineTypeId";
    private const string AlertsUniqueCycleIndex = "UX_Alerts_OnePerCycleAndType";

    private const string RoutineTypesTable = "RoutineTypes";
    private const string PlansTable = "MaintenancePlans";
    private const string RoutinesTable = "MaintenanceRoutines";
    private const string AlertsTable = "Alerts";
    private const string ReadingsTable = "OdometerReadings";
    private const string StatusColumn = "Status";
    private const string StateColumn = "State";
    private const string DueStatusColumn = "DueStatus";
    private const string LastTriggerColumn = "LastTrigger";
    private const string TypeColumn = "Type";
    private const string TriggeredByColumn = "TriggeredBy";
    private const string ResolutionReasonColumn = "ResolutionReason";
    private const string OutcomeColumn = "Outcome";
    private const string IgnoredReasonColumn = "IgnoredReason";
    private const string CheckQuery =
        "SELECT cc.definition AS [Value] FROM sys.check_constraints cc "
        + "JOIN sys.tables t ON t.object_id = cc.parent_object_id "
        + "JOIN sys.schemas s ON s.schema_id = t.schema_id "
        + "WHERE s.name = {0} AND t.name = {1}";
    private const string UniqueIndexQuery =
        "SELECT i.name AS [Value] FROM sys.indexes i "
        + "JOIN sys.tables t ON t.object_id = i.object_id "
        + "JOIN sys.schemas s ON s.schema_id = t.schema_id "
        + "WHERE s.name = {0} AND i.is_unique = 1 AND i.name IS NOT NULL";
    private const string ColumnReferenceFormat = "[{0}]";
    private const string LiteralPattern = "'([^']+)'";
    private const string TodayText = "2026-11-12";
    private const string OilServiceDateText = "2026-08-01";
    private const string ReadAtText = "2026-11-12T08:00:00-05:00";
    private const string PlateText = "ABC123";
    private const long OdometerKilometers = 52300L;
    private const long OilServiceKilometers = 45000L;
    private const int BrakesMonthsOverride = 18;
    private const decimal BrakesPercentOverride = 20m;
    private const int SeededTypeCount = 3;
    private const int RegisteredRoutineCount = 2;
    private const int OilChangeKilometers = 10000;
    private const int OilChangeMonths = 6;
    private const decimal DefaultPercent = 10m;

    public static TheoryData<string, string, Type> EnumColumns => new()
    {
        { RoutineTypesTable, StatusColumn, typeof(RoutineTypeStatus) },
        { PlansTable, StatusColumn, typeof(MaintenancePlanStatus) },
        { RoutinesTable, StateColumn, typeof(RoutineState) },
        { RoutinesTable, DueStatusColumn, typeof(RoutineDueStatus) },
        { RoutinesTable, LastTriggerColumn, typeof(AlertTrigger) },
        { AlertsTable, TypeColumn, typeof(AlertType) },
        { AlertsTable, TriggeredByColumn, typeof(AlertTrigger) },
        { AlertsTable, StatusColumn, typeof(AlertStatus) },
        { AlertsTable, ResolutionReasonColumn, typeof(AlertResolutionReason) },
        { ReadingsTable, OutcomeColumn, typeof(OdometerReadingOutcome) },
        { ReadingsTable, IgnoredReasonColumn, typeof(OdometerReadingIgnoredReason) }
    };

    [Theory]
    [MemberData(nameof(EnumColumns))]
    public async Task CreateSchema_Should_MatchEnumNames_When_CheckConstraintExists(string table, string column, Type enumType)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        List<string> definitions = await QueryAsync(CheckQuery, cancellationToken, MaintenanceSchema.Name, table);

        string columnReference = string.Format(CultureInfo.InvariantCulture, ColumnReferenceFormat, column);
        string definition = definitions.Single(candidate => candidate.Contains(columnReference, StringComparison.Ordinal));
        List<string> literals = Regex.Matches(definition, LiteralPattern).Select(match => match.Groups[1].Value).ToList();
        literals.Should().BeEquivalentTo(Enum.GetNames(enumType));
    }

    [Theory]
    [InlineData(PlansUniquePlateIndex)]
    [InlineData(RoutinesUniqueTypeIndex)]
    [InlineData(AlertsUniqueCycleIndex)]
    public async Task CreateSchema_Should_CreateUniqueIndex_When_DatabaseIsCreated(string indexName)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        List<string> uniqueIndexes = await QueryUniqueIndexesAsync(cancellationToken);

        uniqueIndexes.Should().Contain(indexName);
    }

    [Fact]
    public async Task CreateSchema_Should_SeedRoutineTypes_When_DatabaseIsCreated()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        List<RoutineType> types = await context.RoutineTypes.AsNoTracking().ToListAsync(cancellationToken);

        types.Should().HaveCount(SeededTypeCount);
        RoutineType oilChange = types.Single(type => type.Id == RoutineTypeSeed.OilChangeId);
        oilChange.Code.Value.Should().Be(RoutineTypeSeed.OilChangeCode);
        oilChange.DefaultInterval.Kilometers.Should().Be(OilChangeKilometers);
        oilChange.DefaultInterval.Months.Should().Be(OilChangeMonths);
        oilChange.DueSoonThreshold.Percent.Should().Be(DefaultPercent);
    }

    [Fact]
    public async Task SaveChanges_Should_RoundTripPlanWithRoutines_When_PlanIsReloaded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan plan = await SavePlanWithRoutinesAsync(cancellationToken);

        await using MaintenanceDbContext readContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        MaintenancePlan loaded = await readContext.MaintenancePlans
            .Include(candidate => candidate.Routines)
            .SingleAsync(candidate => candidate.Id == plan.Id, cancellationToken);

        loaded.Plate.Should().Be(plan.Plate);
        loaded.CustomerId.Should().Be(plan.CustomerId);
        loaded.Status.Should().Be(plan.Status);
        loaded.Odometer.Should().Be(plan.Odometer);
        loaded.LastTimeEvaluationDate.Should().Be(plan.LastTimeEvaluationDate);
        loaded.Routines.Should().HaveCount(RegisteredRoutineCount);
        foreach (MaintenanceRoutine expected in plan.Routines)
        {
            AssertRoutine(loaded.Routines.Single(candidate => candidate.Id == expected.Id), expected);
        }
    }

    private static void AssertRoutine(MaintenanceRoutine actual, MaintenanceRoutine expected)
    {
        actual.RoutineTypeId.Should().Be(expected.RoutineTypeId);
        actual.RoutineTypeCode.Should().Be(expected.RoutineTypeCode);
        actual.Name.Should().Be(expected.Name);
        actual.Overrides.Should().Be(expected.Overrides);
        actual.Cycle.Should().Be(expected.Cycle);
        actual.State.Should().Be(expected.State);
        actual.DueStatus.Should().Be(expected.DueStatus);
        actual.DueSoonNotified.Should().Be(expected.DueSoonNotified);
        actual.LastTrigger.Should().Be(expected.LastTrigger);
    }

    private static MaintenancePlan CreatePlan()
    {
        DateTimeOffset readAt = DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture);
        return MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(PlateText).Value,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            Odometer.Create(OdometerKilometers, readAt).Value,
            DateOnly.Parse(TodayText, CultureInfo.InvariantCulture));
    }

    private static void RegisterRoutines(MaintenancePlan plan, RoutineParameterCatalog catalog)
    {
        DateOnly today = DateOnly.Parse(TodayText, CultureInfo.InvariantCulture);
        DateOnly oilServiceDate = DateOnly.Parse(OilServiceDateText, CultureInfo.InvariantCulture);
        RoutineRegistration oilChange = RoutineRegistration.Create(
            RoutineTypeSeed.OilChangeId,
            OilServiceKilometers,
            oilServiceDate,
            RoutineOverrides.None);
        RoutineRegistration brakes = RoutineRegistration.Create(
            RoutineTypeSeed.BrakesId,
            lastServiceKm: null,
            lastServiceDate: null,
            RoutineOverrides.Create(null, BrakesMonthsOverride, BrakesPercentOverride).Value);

        plan.RegisterRoutine(oilChange, catalog, today).IsSuccess.Should().BeTrue();
        plan.RegisterRoutine(brakes, catalog, today).IsSuccess.Should().BeTrue();
    }

    private async Task<MaintenancePlan> SavePlanWithRoutinesAsync(CancellationToken cancellationToken)
    {
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        List<RoutineType> types = await context.RoutineTypes.ToListAsync(cancellationToken);
        MaintenancePlan plan = CreatePlan();
        RegisterRoutines(plan, RoutineParameterCatalog.From(types));
        context.MaintenancePlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    private async Task<List<string>> QueryUniqueIndexesAsync(CancellationToken cancellationToken) =>
        await QueryAsync(UniqueIndexQuery, cancellationToken, MaintenanceSchema.Name);

    private async Task<List<string>> QueryAsync(string sql, CancellationToken cancellationToken, params object[] parameters)
    {
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        return await context.Database.SqlQueryRaw<string>(sql, parameters).ToListAsync(cancellationToken);
    }
}
