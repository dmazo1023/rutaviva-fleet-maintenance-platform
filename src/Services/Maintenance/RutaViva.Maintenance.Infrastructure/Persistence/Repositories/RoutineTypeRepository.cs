using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

public sealed class RoutineTypeRepository(MaintenanceDbContext context) : IRoutineTypeRepository
{
    public async Task<RoutineType?> GetAsync(RoutineTypeId id, CancellationToken cancellationToken) =>
        await context.RoutineTypes.FirstOrDefaultAsync(routineType => routineType.Id == id, cancellationToken);

    public async Task<RoutineParameterCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        List<RoutineType> routineTypes = await context.RoutineTypes.ToListAsync(cancellationToken);
        return RoutineParameterCatalog.From(routineTypes);
    }
}
