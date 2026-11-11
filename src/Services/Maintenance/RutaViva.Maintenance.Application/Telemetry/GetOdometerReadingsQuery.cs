using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed record GetOdometerReadingsQuery(VehicleId VehicleId, int Limit)
    : IQuery<IReadOnlyList<OdometerReadingDto>>;
