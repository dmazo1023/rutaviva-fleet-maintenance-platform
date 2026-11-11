using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.RoutineTypes;

public sealed class RoutineParameterCatalogTests
{
    private const string NotFoundCode = "RoutineType.NotFound";
    private const string OilChangeCode = "OIL_CHANGE";
    private const string BrakesCode = "BRAKES";
    private const string TiresCode = "TIRES";
    private const string LowerCaseBrakes = "brakes";
    private const string OilChangeName = "Oil change";
    private const string BrakesName = "Brakes";
    private const string Seed = "seed";
    private const int OilChangeKm = 10000;
    private const int OilChangeMonths = 6;
    private const int BrakesKm = 20000;
    private const int BrakesMonths = 12;
    private const decimal TenPercent = 10m;
    private const int SeedYear = 2026;
    private const int SeedMonth = 1;
    private const int SeedDay = 1;

    private static readonly DateTimeOffset SeedAt = new(SeedYear, SeedMonth, SeedDay, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Find_Should_ReturnParameters_When_CodeExists()
    {
        RoutineParameterCatalog catalog = BuildCatalog();

        Result<RoutineTypeParameters> result = catalog.Find(CodeOf(BrakesCode));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(BrakesName);
        result.Value.Interval.Kilometers.Should().Be(BrakesKm);
        result.Value.Interval.Months.Should().Be(BrakesMonths);
        result.Value.Threshold.Percent.Should().Be(TenPercent);
    }

    [Fact]
    public void FindByCode_Should_ReturnParameters_When_CodeIsGivenInLowerCase()
    {
        RoutineParameterCatalog catalog = BuildCatalog();

        Result<RoutineTypeParameters> result = catalog.FindByCode(CodeOf(LowerCaseBrakes));

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Value.Should().Be(BrakesCode);
    }

    [Fact]
    public void Find_Should_ReturnNotFound_When_CodeDoesNotExist()
    {
        RoutineParameterCatalog catalog = BuildCatalog();

        Result<RoutineTypeParameters> result = catalog.Find(CodeOf(TiresCode));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NotFoundCode);
    }

    [Fact]
    public void Find_Should_ReturnParameters_When_IdExists()
    {
        RoutineType oilChange = CreateType(OilChangeCode, OilChangeName, OilChangeKm, OilChangeMonths);
        RoutineParameterCatalog catalog = RoutineParameterCatalog.From([oilChange]);

        Result<RoutineTypeParameters> result = catalog.Find(oilChange.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Value.Should().Be(OilChangeCode);
    }

    [Fact]
    public void Find_Should_ReturnNotFound_When_IdDoesNotExist()
    {
        RoutineParameterCatalog catalog = BuildCatalog();

        Result<RoutineTypeParameters> result = catalog.Find(RoutineTypeId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NotFoundCode);
    }

    [Fact]
    public void Find_Should_ReturnNotFound_When_CatalogIsEmpty()
    {
        RoutineParameterCatalog catalog = RoutineParameterCatalog.From([]);

        Result<RoutineTypeParameters> result = catalog.Find(CodeOf(OilChangeCode));

        result.Error.Code.Should().Be(NotFoundCode);
        catalog.Count.Should().Be(0);
    }

    private static RoutineParameterCatalog BuildCatalog() =>
        RoutineParameterCatalog.From(
        [
            CreateType(OilChangeCode, OilChangeName, OilChangeKm, OilChangeMonths),
            CreateType(BrakesCode, BrakesName, BrakesKm, BrakesMonths)
        ]);

    private static RoutineType CreateType(string code, string name, int kilometers, int months) =>
        RoutineType.Create(
            RoutineTypeId.New(),
            CodeOf(code),
            name,
            MaintenanceInterval.Create(kilometers, months).Value,
            DueSoonThreshold.Create(TenPercent).Value,
            Seed,
            SeedAt).Value;

    private static RoutineTypeCode CodeOf(string code) => RoutineTypeCode.Create(code).Value;
}
