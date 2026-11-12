namespace RutaViva.Customers.IntegrationTests.Api;

internal static class SampleDataLocator
{
    private const string SolutionFile = "RutaViva.slnx";
    private const string NotFoundMessage = "Repository root with RutaViva.slnx was not found.";
    private const string SamplesFolder = "samples";
    private const string ImportFolder = "import";

    public static string ImportFile(string fileName) => Path.Combine(RootPath(), SamplesFolder, ImportFolder, fileName);

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
