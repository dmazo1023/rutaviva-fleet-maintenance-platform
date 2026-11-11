using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IOdometerReadingQueries
{
    Task<IReadOnlyList<OdometerReadingDto>> GetRecentAsync(VehicleId id, int limit, CancellationToken cancellationToken);
}
