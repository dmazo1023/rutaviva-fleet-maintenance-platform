using Microsoft.Data.SqlClient;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public static class DeadLetterQuery
{
    private const string SubscriberParameter = "@subscriber";
    private const string CountQuery = "SELECT COUNT(*) FROM eventbus.DeadLetters WHERE SubscriberName = @subscriber";

    public static async Task<int> CountAsync(string connectionString, string subscriber, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(CountQuery, connection);
        command.Parameters.AddWithValue(SubscriberParameter, subscriber);
        object? count = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(count);
    }
}
