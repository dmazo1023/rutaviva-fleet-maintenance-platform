using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Application.Telemetry.Queries;

public sealed class GetOdometerReadingsHandler(IOdometerReadingQueries readings)
    : IQueryHandler<GetOdometerReadingsQuery, IReadOnlyList<OdometerReadingDto>>
{
    public async Task<Result<IReadOnlyList<OdometerReadingDto>>> HandleAsync(
        GetOdometerReadingsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OdometerReadingDto> recent = await readings.GetRecentAsync(query.VehicleId, query.Limit, cancellationToken);
        return Result<IReadOnlyList<OdometerReadingDto>>.Success(recent);
    }
}
