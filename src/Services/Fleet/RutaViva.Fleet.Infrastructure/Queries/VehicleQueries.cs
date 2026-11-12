using Microsoft.EntityFrameworkCore;
using RutaViva.Fleet.Application.Vehicles.Queries;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.Fleet.Infrastructure.Persistence;

namespace RutaViva.Fleet.Infrastructure.Queries;

public sealed class VehicleQueries(FleetDbContext context) : IVehicleQueries
{
    public async Task<PagedResult<VehicleSummaryDto>> ListAsync(VehicleListCriteria criteria, CancellationToken cancellationToken)
    {
        IQueryable<Vehicle> filtered = Filter(context.Vehicles.AsNoTracking(), criteria);
        int totalCount = await filtered.CountAsync(cancellationToken);
        List<VehicleReadRow> rows = await Rows(Page(filtered, criteria)).ToListAsync(cancellationToken);
        List<VehicleSummaryDto> items = rows
            .OrderByDescending(row => row.Vehicle.RegisteredAt)
            .ThenByDescending(row => row.Vehicle.Id.Value)
            .Select(VehicleProjector.ToSummary)
            .ToList();

        return new PagedResult<VehicleSummaryDto>(items, criteria.Page, criteria.PageSize, totalCount);
    }

    public async Task<VehicleDetailDto?> GetDetailAsync(VehicleId id, CancellationToken cancellationToken)
    {
        IQueryable<Vehicle> vehicles = context.Vehicles.AsNoTracking().Where(vehicle => vehicle.Id == id);
        return await FindDetailAsync(vehicles, cancellationToken);
    }

    public async Task<VehicleDetailDto?> GetByPlateAsync(Plate plate, CancellationToken cancellationToken)
    {
        IQueryable<Vehicle> vehicles = context.Vehicles.AsNoTracking().Where(vehicle => vehicle.Plate == plate);
        return await FindDetailAsync(vehicles, cancellationToken);
    }

    private static IQueryable<Vehicle> Filter(IQueryable<Vehicle> vehicles, VehicleListCriteria criteria) =>
        FilterBySearch(FilterByStatus(FilterByCustomer(vehicles, criteria.CustomerId), criteria.Status), criteria.Search);

    private static IQueryable<Vehicle> FilterByCustomer(IQueryable<Vehicle> vehicles, CustomerId? customerId)
    {
        if (customerId is not { } value)
        {
            return vehicles;
        }

        return vehicles.Where(vehicle => vehicle.CustomerId == value);
    }

    private static IQueryable<Vehicle> FilterByStatus(IQueryable<Vehicle> vehicles, VehicleStatus? status)
    {
        if (status is not { } value)
        {
            return vehicles;
        }

        return vehicles.Where(vehicle => vehicle.Status == value);
    }

    private static IQueryable<Vehicle> FilterBySearch(IQueryable<Vehicle> vehicles, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return vehicles;
        }

        string term = search.Trim().ToUpperInvariant();
        return vehicles.Where(vehicle =>
            ((string)(object)vehicle.Plate).ToUpper().Contains(term)
            || vehicle.Description.Brand.ToUpper().Contains(term)
            || vehicle.Description.Model.ToUpper().Contains(term));
    }

    private static IQueryable<Vehicle> Page(IQueryable<Vehicle> vehicles, VehicleListCriteria criteria) =>
        vehicles
            .OrderByDescending(vehicle => vehicle.RegisteredAt)
            .ThenByDescending(vehicle => vehicle.Id)
            .Skip((criteria.Page - VehicleListDefaults.FirstPage) * criteria.PageSize)
            .Take(criteria.PageSize);

    private async Task<VehicleDetailDto?> FindDetailAsync(IQueryable<Vehicle> vehicles, CancellationToken cancellationToken)
    {
        VehicleReadRow? row = await Rows(vehicles).FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        return VehicleProjector.ToDetail(row);
    }

    private IQueryable<VehicleReadRow> Rows(IQueryable<Vehicle> vehicles) =>
        from vehicle in vehicles
        join owner in context.CustomerReferences.AsNoTracking() on vehicle.CustomerId equals owner.Id
        join workshop in context.WorkshopReferences.AsNoTracking() on vehicle.WorkshopId equals workshop.Id
        join approver in context.ApproverReferences.AsNoTracking() on vehicle.ResponsibleApproverId equals (ApproverId?)approver.Id into approvers
        from responsible in approvers.DefaultIfEmpty()
        select new VehicleReadRow(vehicle, owner, workshop, responsible);
}
