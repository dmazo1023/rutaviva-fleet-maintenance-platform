namespace RutaViva.Architecture.Tests;

internal static class RepositoryLocator
{
    private const string SolutionFile = "RutaViva.slnx";
    private const string NotFoundMessage = "Repository root with RutaViva.slnx was not found.";

    public static string RootPath()
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
