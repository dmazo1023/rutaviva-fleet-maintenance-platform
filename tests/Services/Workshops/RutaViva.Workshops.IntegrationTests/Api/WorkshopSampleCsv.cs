using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Workshops.IntegrationTests.Api;

internal static class WorkshopSampleCsv
{
    private const string SolutionFile = "RutaViva.slnx";
    private const string NotFoundMessage = "Repository root with RutaViva.slnx was not found.";
    private const string SamplesFolder = "samples";
    private const string ImportFolder = "import";
    private const string WorkshopsFile = "talleres.csv";
    private const char Separator = ';';
    private const int HeaderLines = 1;
    private const int FirstRowNumber = 1;
    private const int TaxIdColumn = 0;
    private const int NameColumn = 1;
    private const int EmailColumn = 2;
    private const int PhoneColumn = 3;
    private const int AddressColumn = 4;
    private const int CityColumn = 5;

    public static IReadOnlyList<WorkshopBulkRow> ReadWorkshops() =>
        File.ReadAllLines(WorkshopsPath())
            .Skip(HeaderLines)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ToRow)
            .ToList();

    private static WorkshopBulkRow ToRow(string line, int index)
    {
        string[] columns = line.Split(Separator);
        return new WorkshopBulkRow(
            index + FirstRowNumber,
            columns[TaxIdColumn],
            columns[NameColumn],
            columns[EmailColumn],
            columns[PhoneColumn],
            columns[AddressColumn],
            columns[CityColumn]);
    }

    private static string WorkshopsPath() => Path.Combine(RootPath(), SamplesFolder, ImportFolder, WorkshopsFile);

    private static string RootPath()
    {
        for (DirectoryInfo? current = new(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFile)))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(NotFoundMessage);
    }
}
