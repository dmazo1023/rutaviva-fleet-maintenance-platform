using RutaViva.Maintenance.Application.RoutineTypes;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IRoutineTypeQueries
{
    Task<IReadOnlyList<RoutineTypeDto>> GetAllAsync(CancellationToken cancellationToken);
}
