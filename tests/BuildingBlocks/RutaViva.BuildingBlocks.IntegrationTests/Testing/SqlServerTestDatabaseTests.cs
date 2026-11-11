using Microsoft.Data.SqlClient;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.Testing;

[Trait("Category", "Integration")]
public sealed class SqlServerTestDatabaseTests
{
    private const string ExistsQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
    private const string NameParameter = "@name";
    private const string SelectOne = "SELECT 1";
    private const int ExpectedOne = 1;
    private const int ExpectedNone = 0;

    [Fact]
    public async Task Database_Should_BeUsableAndDroppedAfterDispose_When_Lifecycle_Completes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlServerTestDatabase database = new();
        await database.InitializeAsync();

        int scalar = await ExecuteScalarAsync(database.ConnectionString, SelectOne, cancellationToken);
        int existedBefore = await CountDatabasesAsync(database, cancellationToken);
        await database.DisposeAsync();
        int existsAfter = await CountDatabasesAsync(database, cancellationToken);

        scalar.Should().Be(ExpectedOne);
        existedBefore.Should().Be(ExpectedOne);
        existsAfter.Should().Be(ExpectedNone);
    }

    private static async Task<int> CountDatabasesAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(database.MasterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(ExistsQuery, connection);
        command.Parameters.AddWithValue(NameParameter, database.DatabaseName);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<int> ExecuteScalarAsync(string connectionString, string query, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(query, connection);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
