using System.Globalization;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

internal static class SampleCsv
{
    private const char Separator = ';';
    private const string VehiclesFile = "vehiculos.csv";
    private const string RoutinesFile = "rutinas.csv";
    private const string DateFormat = "yyyy-MM-dd";
    private const int HeaderRows = 1;
    private const int FirstRowNumber = 1;
    private const int PlateColumn = 0;
    private const int VinColumn = 1;
    private const int BrandColumn = 2;
    private const int ModelColumn = 3;
    private const int YearColumn = 4;
    private const int OwnerColumn = 5;
    private const int WorkshopColumn = 6;
    private const int VehicleOdometerColumn = 8;
    private const int RoutineCodeColumn = 1;
    private const int LastServiceKmColumn = 2;
    private const int LastServiceDateColumn = 3;
    private const int IntervalKmColumn = 4;
    private const int IntervalMonthsColumn = 5;
    private const int ThresholdColumn = 6;

    public static IReadOnlyList<SampleVehicle> Vehicles() =>
        ReadRows(VehiclesFile).Select(ToVehicle).ToList();

    public static IReadOnlyList<RoutineBulkRow> Routines() =>
        ReadRows(RoutinesFile).Select((fields, index) => ToRoutine(fields, index + FirstRowNumber)).ToList();

    private static IEnumerable<string[]> ReadRows(string fileName) =>
        File.ReadAllLines(SampleDataLocator.ImportFile(fileName))
            .Skip(HeaderRows)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(Separator));

    private static SampleVehicle ToVehicle(string[] fields) =>
        new(
            fields[PlateColumn],
            NullIfEmpty(fields[VinColumn]),
            fields[BrandColumn],
            fields[ModelColumn],
            int.Parse(fields[YearColumn], CultureInfo.InvariantCulture),
            fields[OwnerColumn],
            fields[WorkshopColumn],
            long.Parse(fields[VehicleOdometerColumn], CultureInfo.InvariantCulture));

    private static RoutineBulkRow ToRoutine(string[] fields, int rowNumber) =>
        new(
            rowNumber,
            fields[PlateColumn],
            fields[RoutineCodeColumn],
            ParseLong(fields[LastServiceKmColumn]),
            ParseDate(fields[LastServiceDateColumn]),
            ParseInt(fields[IntervalKmColumn]),
            ParseInt(fields[IntervalMonthsColumn]),
            ParseInt(fields[ThresholdColumn]));

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static long? ParseLong(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : long.Parse(value, CultureInfo.InvariantCulture);

    private static int? ParseInt(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : int.Parse(value, CultureInfo.InvariantCulture);

    private static DateOnly? ParseDate(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);
}
