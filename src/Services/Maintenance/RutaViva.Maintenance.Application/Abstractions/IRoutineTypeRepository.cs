using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IRoutineTypeRepository
{
    Task<RoutineType?> GetAsync(RoutineTypeId id, CancellationToken cancellationToken);

    Task<RoutineParameterCatalog> GetCatalogAsync(CancellationToken cancellationToken);
}
