using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RutaViva.Testing.Fixtures;

public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private const string ConnectionVariable = "RUTAVIVA_TEST_SQL";
    private const string DefaultConnection = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string DatabasePrefix = "RutaViva_Tests_";
    private const string MasterDatabase = "master";
    private const string GuidFormat = "N";
    private const int AdministrativeCommandTimeoutSeconds = 300;
    private const int StaleAfterHours = 2;
    private const string StaleQuery = "SELECT name FROM sys.databases WHERE name LIKE 'RutaViva[_]Tests[_]%' AND create_date < DATEADD(HOUR, -@hours, GETDATE())";
    private const string HoursParameter = "@hours";
    private const string CreateCommand = "CREATE DATABASE [{0}]";
    private const string DropCommand = "IF DB_ID(N'{0}') IS NOT NULL BEGIN ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{0}]; END";

    private readonly string _masterConnectionString = BuildMasterConnectionString();

    public string DatabaseName { get; } = DatabasePrefix + Guid.NewGuid().ToString(GuidFormat);

    public string MasterConnectionString => _masterConnectionString;

    public string ConnectionString =>
        new SqlConnectionStringBuilder(_masterConnectionString) { InitialCatalog = DatabaseName }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await DropStaleDatabasesAsync(cancellationToken);
        await ExecuteOnMasterAsync(string.Format(CreateCommand, DatabaseName), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await ExecuteOnMasterAsync(string.Format(DropCommand, DatabaseName), CancellationToken.None);
    }

    public async Task<TContext> CreateDbContextAsync<TContext>(CancellationToken cancellationToken)
        where TContext : DbContext
    {
        DbContextOptions<TContext> options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(ConnectionString, sql => sql.CommandTimeout(AdministrativeCommandTimeoutSeconds))
            .Options;
        TContext context = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        await PrepareSchemaAsync(context, cancellationToken);
        return context;
    }

    private static async Task PrepareSchemaAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (!context.Database.GetMigrations().Any())
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await context.Database.MigrateAsync(cancellationToken);
    }

    private static string BuildMasterConnectionString()
    {
        string? configured = Environment.GetEnvironmentVariable(ConnectionVariable);
        string baseConnection = string.IsNullOrWhiteSpace(configured) ? DefaultConnection : configured;
        return new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = MasterDatabase }.ConnectionString;
    }

    private async Task DropStaleDatabasesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> staleNames = await ReadStaleNamesAsync(cancellationToken);
        foreach (string name in staleNames)
        {
            await ExecuteOnMasterAsync(string.Format(DropCommand, name), cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<string>> ReadStaleNamesAsync(CancellationToken cancellationToken)
    {
        List<string> names = [];
        await using SqlConnection connection = new(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(StaleQuery, connection);
        command.Parameters.AddWithValue(HoursParameter, StaleAfterHours);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private async Task ExecuteOnMasterAsync(string commandText, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(commandText, connection) { CommandTimeout = AdministrativeCommandTimeoutSeconds };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
