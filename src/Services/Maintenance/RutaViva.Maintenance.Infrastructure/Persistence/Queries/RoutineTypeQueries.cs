using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.RoutineTypes;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

public sealed class RoutineTypeQueries(MaintenanceDbContext context) : IRoutineTypeQueries
{
    public async Task<IReadOnlyList<RoutineTypeDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.RoutineTypes
            .AsNoTracking()
            .OrderBy(routineType => routineType.Name)
            .Select(routineType => new RoutineTypeDto(
                routineType.Id,
                routineType.Code.Value,
                routineType.Name,
                routineType.DefaultInterval.Kilometers,
                routineType.DefaultInterval.Months,
                routineType.DueSoonThreshold.Percent,
                routineType.Status,
                routineType.UpdatedBy,
                routineType.UpdatedAt))
            .ToListAsync(cancellationToken);
}
