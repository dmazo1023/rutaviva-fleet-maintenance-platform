using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IMaintenancePlanRepository
{
    Task<MaintenancePlan?> GetAsync(VehicleId id, CancellationToken cancellationToken);

    Task<VehicleId?> FindVehicleIdByPlateAsync(LicensePlate plate, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleId>> GetActivePlanIdsPageAsync(VehicleId? after, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleId>> GetActivePlanIdsWithRoutineTypeAsync(RoutineTypeId routineTypeId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<LicensePlate, MaintenancePlan>> GetByPlatesAsync(
        IReadOnlyCollection<LicensePlate> plates,
        CancellationToken cancellationToken);

    void Add(MaintenancePlan plan);
}
