using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.Fleet.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Api;

internal static class FleetOutboxReader
{
    private const string VehicleRegisteredFragment = "VehicleRegistered";

    public static async Task<int> CountVehicleRegisteredAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        return await context.Set<OutboxMessage>()
            .AsNoTracking()
            .CountAsync(message => message.EventType.Contains(VehicleRegisteredFragment), cancellationToken);
    }
}
