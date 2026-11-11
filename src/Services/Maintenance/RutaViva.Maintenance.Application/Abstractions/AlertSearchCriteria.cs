using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Abstractions;

public sealed record AlertSearchCriteria(
    AlertStatus? Status,
    VehicleId? VehicleId,
    CustomerId? CustomerId,
    int Page,
    int PageSize);
