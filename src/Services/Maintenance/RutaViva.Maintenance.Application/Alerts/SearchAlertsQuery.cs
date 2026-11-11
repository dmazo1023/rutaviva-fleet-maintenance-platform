using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed record SearchAlertsQuery(
    AlertStatus? Status,
    VehicleId? VehicleId,
    CustomerId? CustomerId,
    int Page,
    int PageSize) : IQuery<PagedResult<AlertDto>>;
