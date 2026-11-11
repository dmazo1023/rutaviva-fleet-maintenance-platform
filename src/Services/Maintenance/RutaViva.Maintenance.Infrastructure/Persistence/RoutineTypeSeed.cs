using System.Globalization;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence;

public static class RoutineTypeSeed
{
    public const string OilChangeCode = "OIL_CHANGE";
    public const string BrakesCode = "BRAKES";
    public const string TiresCode = "TIRES";

    public static readonly RoutineTypeId OilChangeId = new(Guid.Parse(OilChangeGuid));
    public static readonly RoutineTypeId BrakesId = new(Guid.Parse(BrakesGuid));
    public static readonly RoutineTypeId TiresId = new(Guid.Parse(TiresGuid));

    private const string OilChangeGuid = "7f3c1a10-0001-4000-8000-000000000001";
    private const string BrakesGuid = "7f3c1a10-0001-4000-8000-000000000002";
    private const string TiresGuid = "7f3c1a10-0001-4000-8000-000000000003";
    private const string OilChangeName = "Cambio de aceite";
    private const string BrakesName = "Frenos";
    private const string TiresName = "Llantas";
    private const int OilChangeKilometers = 10000;
    private const int OilChangeMonths = 6;
    private const int BrakesKilometers = 20000;
    private const int BrakesMonths = 12;
    private const int TiresKilometers = 40000;
    private const int TiresMonths = 24;
    private const decimal DefaultDueSoonPercent = 10m;
    private const string SeedAuthor = "system";
    private const string SeededAtText = "2026-11-01T00:00:00+00:00";

    private static readonly DateTimeOffset SeededAt = DateTimeOffset.Parse(SeededAtText, CultureInfo.InvariantCulture);
    private static readonly DueSoonThreshold DefaultThreshold = DueSoonThreshold.Create(DefaultDueSoonPercent).Value;

    private static readonly SeedDefinition[] Definitions =
    [
        new(OilChangeId, OilChangeCode, OilChangeName, OilChangeKilometers, OilChangeMonths),
        new(BrakesId, BrakesCode, BrakesName, BrakesKilometers, BrakesMonths),
        new(TiresId, TiresCode, TiresName, TiresKilometers, TiresMonths)
    ];

    internal static IReadOnlyList<object> TypeRows() => Definitions.Select(ToTypeRow).ToList();

    internal static IReadOnlyList<object> IntervalRows() => Definitions.Select(ToIntervalRow).ToList();

    private static object ToTypeRow(SeedDefinition definition) => new
    {
        definition.Id,
        Code = RoutineTypeCode.Create(definition.Code).Value,
        definition.Name,
        DueSoonThreshold = DefaultThreshold,
        Status = RoutineTypeStatus.Active,
        UpdatedBy = SeedAuthor,
        UpdatedAt = SeededAt
    };

    private static object ToIntervalRow(SeedDefinition definition) => new
    {
        RoutineTypeId = definition.Id,
        Kilometers = (int?)definition.Kilometers,
        Months = (int?)definition.Months
    };

    private sealed record SeedDefinition(RoutineTypeId Id, string Code, string Name, int Kilometers, int Months);
}
