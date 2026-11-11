using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

public sealed class MaintenancePlanRepository(MaintenanceDbContext context) : IMaintenancePlanRepository
{
    private const string PlansAfterSql = "SELECT * FROM [" + MaintenanceSchema.Name + "].[MaintenancePlans] WHERE [VehicleId] > {0}";

    public async Task<MaintenancePlan?> GetAsync(VehicleId id, CancellationToken cancellationToken) =>
        await context.MaintenancePlans
            .Include(plan => plan.Routines)
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);

    public async Task<VehicleId?> FindVehicleIdByPlateAsync(LicensePlate plate, CancellationToken cancellationToken)
    {
        List<VehicleId> ids = await context.MaintenancePlans
            .AsNoTracking()
            .Where(plan => plan.Plate == plate)
            .Select(plan => plan.Id)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return null;
        }

        return ids[0];
    }

    public async Task<IReadOnlyList<VehicleId>> GetActivePlanIdsPageAsync(
        VehicleId? after,
        int pageSize,
        CancellationToken cancellationToken)
    {
        Guid lowerBound = after?.Value ?? Guid.Empty;
        return await context.MaintenancePlans
            .FromSqlRaw(PlansAfterSql, lowerBound)
            .AsNoTracking()
            .Where(plan => plan.Status == MaintenancePlanStatus.Active)
            .OrderBy(plan => plan.Id)
            .Select(plan => plan.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VehicleId>> GetActivePlanIdsWithRoutineTypeAsync(
        RoutineTypeId routineTypeId,
        CancellationToken cancellationToken) =>
        await context.MaintenancePlans
            .AsNoTracking()
            .Where(plan => plan.Status == MaintenancePlanStatus.Active)
            .Where(plan => plan.Routines.Any(routine => routine.RoutineTypeId == routineTypeId))
            .OrderBy(plan => plan.Id)
            .Select(plan => plan.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<LicensePlate, MaintenancePlan>> GetByPlatesAsync(
        IReadOnlyCollection<LicensePlate> plates,
        CancellationToken cancellationToken)
    {
        if (plates.Count == 0)
        {
            return new Dictionary<LicensePlate, MaintenancePlan>();
        }

        List<LicensePlate> requested = plates.Distinct().ToList();
        List<MaintenancePlan> plans = await context.MaintenancePlans
            .Include(plan => plan.Routines)
            .Where(plan => requested.Contains(plan.Plate))
            .ToListAsync(cancellationToken);
        return plans.ToDictionary(plan => plan.Plate);
    }

    public void Add(MaintenancePlan plan) => context.MaintenancePlans.Add(plan);
}
