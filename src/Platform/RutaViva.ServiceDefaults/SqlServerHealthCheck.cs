using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RutaViva.ServiceDefaults;

public sealed class SqlServerHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private const string ConnectionStringName = "RutaViva";
    private const string ProbeQuery = "SELECT 1";
    private const string MissingConnectionString = "Connection string 'RutaViva' is not configured.";
    private const string Unreachable = "SQL Server is unreachable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy(MissingConnectionString);
        }

        try
        {
            await ExecuteProbeAsync(connectionString, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or ArgumentException)
        {
            return HealthCheckResult.Unhealthy(Unreachable, exception);
        }
    }

    private static async Task ExecuteProbeAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(ProbeQuery, connection);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
