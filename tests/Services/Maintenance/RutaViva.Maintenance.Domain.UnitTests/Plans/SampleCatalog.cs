using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

internal static class SampleCatalog
{
    internal const string OilChangeCode = "OIL_CHANGE";
    internal const string BrakesCode = "BRAKES";
    internal const string TiresCode = "TIRES";
    internal const int OilChangeKm = 10000;
    internal const int OilChangeMonths = 6;

    private const string OilChangeName = "Oil change";
    private const string BrakesName = "Brakes";
    private const string TiresName = "Tires";
    private const int BrakesKm = 20000;
    private const int BrakesMonths = 12;
    private const int TiresKm = 40000;
    private const int TiresMonths = 24;
    private const decimal DefaultPercent = 10m;

    private static readonly RoutineTypeId OilChangeId = RoutineTypeId.New();
    private static readonly RoutineTypeId BrakesId = RoutineTypeId.New();
    private static readonly RoutineTypeId TiresId = RoutineTypeId.New();

    private static readonly IReadOnlyDictionary<string, RoutineTypeId> IdsByCode = new Dictionary<string, RoutineTypeId>
    {
        [OilChangeCode] = OilChangeId,
        [BrakesCode] = BrakesId,
        [TiresCode] = TiresId
    };

    internal static readonly RoutineParameterCatalog Catalog = BuildCatalog();

    internal static RoutineTypeId IdOf(string code) => IdsByCode[code];

    private static RoutineParameterCatalog BuildCatalog() =>
        new(
        [
            KeyValuePair.Create(OilChangeId, ParametersOf(OilChangeCode, OilChangeName, OilChangeKm, OilChangeMonths)),
            KeyValuePair.Create(BrakesId, ParametersOf(BrakesCode, BrakesName, BrakesKm, BrakesMonths)),
            KeyValuePair.Create(TiresId, ParametersOf(TiresCode, TiresName, TiresKm, TiresMonths))
        ]);

    private static RoutineTypeParameters ParametersOf(string code, string name, int kilometers, int months) =>
        new(
            RoutineTypeCode.Create(code).Value,
            name,
            MaintenanceInterval.Create(kilometers, months).Value,
            DueSoonThreshold.Create(DefaultPercent).Value);
}
