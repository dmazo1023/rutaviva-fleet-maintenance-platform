using Microsoft.Data.SqlClient;

namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

public static class SqlSchemaReader
{
    private const string SchemaParameter = "@schema";
    private const string TableParameter = "@table";
    private const string ColumnParameter = "@column";

    private const string ColumnTypeQuery = """
        SELECT CASE
            WHEN ty.name IN ('varchar', 'nvarchar') THEN ty.name + '(' + IIF(c.max_length = -1, 'max', CAST(IIF(ty.name = 'nvarchar', c.max_length / 2, c.max_length) AS varchar(10))) + ')'
            WHEN ty.name = 'decimal' THEN 'decimal(' + CAST(c.precision AS varchar(10)) + ',' + CAST(c.scale AS varchar(10)) + ')'
            WHEN ty.name IN ('datetimeoffset', 'datetime2') THEN ty.name + '(' + CAST(c.scale AS varchar(10)) + ')'
            ELSE ty.name
        END
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE s.name = @schema AND t.name = @table AND c.name = @column
        """;

    private const string FilterQuery = """
        SELECT i.filter_definition
        FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = @schema AND t.name = @table AND i.has_filter = 1
        """;

    public static async Task<string?> ReadColumnTypeAsync(
        string connectionString,
        string schema,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(ColumnTypeQuery, connection);
        command.Parameters.AddWithValue(SchemaParameter, schema);
        command.Parameters.AddWithValue(TableParameter, table);
        command.Parameters.AddWithValue(ColumnParameter, column);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    public static async Task<string?> ReadIndexFilterAsync(
        string connectionString,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(FilterQuery, connection);
        command.Parameters.AddWithValue(SchemaParameter, schema);
        command.Parameters.AddWithValue(TableParameter, table);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }
}
