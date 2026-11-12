namespace RutaViva.Fleet.IntegrationTests.Api;

internal static class SampleImportFiles
{
    public const string CustomersFile = "clientes.csv";
    public const string WorkshopsFile = "talleres.csv";
    public const string ApproversFile = "aprobadores.csv";
    public const string VehiclesFile = "vehiculos.csv";

    private const char Separator = ';';
    private const int HeaderRows = 1;
    private const string SolutionFile = "RutaViva.slnx";
    private const string NotFoundMessage = "Repository root with RutaViva.slnx was not found.";
    private const string SamplesFolder = "samples";
    private const string ImportFolder = "import";

    public static IReadOnlyList<string[]> ReadRows(string fileName) =>
        File.ReadAllLines(Path.Combine(RootPath(), SamplesFolder, ImportFolder, fileName))
            .Skip(HeaderRows)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(Separator))
            .ToList();

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
