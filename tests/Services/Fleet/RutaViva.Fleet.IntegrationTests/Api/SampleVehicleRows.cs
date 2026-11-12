using System.Globalization;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Fleet.IntegrationTests.Api;

internal static class SampleVehicleRows
{
    private const int FirstRowNumber = 1;
    private const int PlateColumn = 0;
    private const int VinColumn = 1;
    private const int BrandColumn = 2;
    private const int ModelColumn = 3;
    private const int YearColumn = 4;
    private const int OwnerColumn = 5;
    private const int WorkshopColumn = 6;
    private const int ResponsibleColumn = 7;
    private const int OdometerColumn = 8;

    public static IReadOnlyList<VehicleBulkRow> Read() =>
        SampleImportFiles.ReadRows(SampleImportFiles.VehiclesFile)
            .Select((fields, index) => ToRow(fields, index + FirstRowNumber))
            .ToList();

    private static VehicleBulkRow ToRow(string[] fields, int rowNumber) =>
        new(
            rowNumber,
            fields[PlateColumn],
            NullIfEmpty(fields[VinColumn]),
            fields[BrandColumn],
            fields[ModelColumn],
            int.Parse(fields[YearColumn], CultureInfo.InvariantCulture),
            fields[OwnerColumn],
            fields[WorkshopColumn],
            NullIfEmpty(fields[ResponsibleColumn]),
            long.Parse(fields[OdometerColumn], CultureInfo.InvariantCulture));

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
