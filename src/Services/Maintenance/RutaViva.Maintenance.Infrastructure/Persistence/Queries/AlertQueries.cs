using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

public sealed class AlertQueries(MaintenanceDbContext context) : IAlertQueries
{
    private const int FirstPage = 1;

    public async Task<PagedResult<AlertDto>> SearchAsync(AlertSearchCriteria criteria, CancellationToken cancellationToken)
    {
        IQueryable<Alert> filtered = Filter(context.Alerts.AsNoTracking(), criteria);
        int totalCount = await filtered.CountAsync(cancellationToken);
        List<AlertDto> items = await Page(filtered, criteria).ToListAsync(cancellationToken);

        return new PagedResult<AlertDto>(items, criteria.Page, criteria.PageSize, totalCount);
    }

    private static IQueryable<Alert> Filter(IQueryable<Alert> alerts, AlertSearchCriteria criteria) =>
        FilterByCustomer(FilterByVehicle(FilterByStatus(alerts, criteria.Status), criteria.VehicleId), criteria.CustomerId);

    private static IQueryable<Alert> FilterByStatus(IQueryable<Alert> alerts, AlertStatus? status)
    {
        if (status is not { } value)
        {
            return alerts;
        }

        return alerts.Where(alert => alert.Status == value);
    }

    private static IQueryable<Alert> FilterByVehicle(IQueryable<Alert> alerts, VehicleId? vehicleId)
    {
        if (vehicleId is not { } value)
        {
            return alerts;
        }

        return alerts.Where(alert => alert.VehicleId == value);
    }

    private static IQueryable<Alert> FilterByCustomer(IQueryable<Alert> alerts, CustomerId? customerId)
    {
        if (customerId is not { } value)
        {
            return alerts;
        }

        return alerts.Where(alert => alert.CustomerId == value);
    }

    private static IQueryable<AlertDto> Page(IQueryable<Alert> alerts, AlertSearchCriteria criteria) =>
        alerts
            .OrderByDescending(alert => alert.RaisedAt)
            .ThenByDescending(alert => alert.Id)
            .Skip((criteria.Page - FirstPage) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(alert => new AlertDto(
                alert.Id,
                alert.VehicleId,
                alert.Plate.Value,
                alert.CustomerId,
                alert.RoutineId,
                alert.RoutineName,
                alert.CycleNumber,
                alert.Type,
                alert.Trigger,
                alert.Status,
                alert.RaisedAt,
                alert.AcknowledgedAt,
                alert.AcknowledgedBy,
                alert.ResolvedAt,
                alert.ResolutionReason));
}
