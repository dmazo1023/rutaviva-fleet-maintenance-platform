using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

public sealed class OdometerReadingQueries(MaintenanceDbContext context) : IOdometerReadingQueries
{
    public async Task<IReadOnlyList<OdometerReadingDto>> GetRecentAsync(VehicleId id, int limit, CancellationToken cancellationToken) =>
        await context.OdometerReadings
            .AsNoTracking()
            .Where(record => record.VehicleId == id)
            .OrderByDescending(record => record.ReadAt)
            .ThenByDescending(record => record.Id)
            .Take(limit)
            .Select(record => new OdometerReadingDto(
                record.Kilometers,
                record.ReadAt,
                record.ReceivedAt,
                record.Outcome,
                record.IgnoredReason))
            .ToListAsync(cancellationToken);
}
