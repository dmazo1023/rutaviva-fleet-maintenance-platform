using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Application.Routines.Bulk;

namespace RutaViva.Maintenance.Application.UnitTests.Routines.Bulk;

public sealed class BulkDuplicateDetectorTests
{
    private const string Plate = "RVA105";
    private const string LowerPlate = " rva105 ";
    private const string OtherPlate = "RVA106";
    private const string OilCode = "OIL_CHANGE";
    private const string LowerOilCode = "oil_change";
    private const string BrakesCode = "BRAKES";
    private const int FirstRow = 1;
    private const int SecondRow = 2;
    private const int ThirdRow = 3;
    private const int SecondIndex = 1;
    private const int ThirdIndex = 2;

    [Fact]
    public void FindDuplicateIndexes_Should_ReturnEmpty_When_NoKeyRepeats()
    {
        IReadOnlyList<RoutineBulkRow> rows = [Row(FirstRow, Plate, OilCode), Row(SecondRow, Plate, BrakesCode), Row(ThirdRow, OtherPlate, OilCode)];

        IReadOnlySet<int> duplicates = BulkDuplicateDetector.FindDuplicateIndexes(rows);

        duplicates.Should().BeEmpty();
    }

    [Fact]
    public void FindDuplicateIndexes_Should_MarkOnlyLaterRows_When_KeyRepeats()
    {
        IReadOnlyList<RoutineBulkRow> rows = [Row(FirstRow, Plate, OilCode), Row(SecondRow, Plate, OilCode), Row(ThirdRow, Plate, OilCode)];

        IReadOnlySet<int> duplicates = BulkDuplicateDetector.FindDuplicateIndexes(rows);

        duplicates.Should().BeEquivalentTo([SecondIndex, ThirdIndex]);
    }

    [Fact]
    public void FindDuplicateIndexes_Should_IgnoreCaseAndWhitespace_When_ComparingKeys()
    {
        IReadOnlyList<RoutineBulkRow> rows = [Row(FirstRow, Plate, OilCode), Row(SecondRow, LowerPlate, LowerOilCode)];

        IReadOnlySet<int> duplicates = BulkDuplicateDetector.FindDuplicateIndexes(rows);

        duplicates.Should().BeEquivalentTo([SecondIndex]);
    }

    private static RoutineBulkRow Row(int rowNumber, string plate, string code) =>
        new(rowNumber, plate, code, null, null, null, null, null);
}
