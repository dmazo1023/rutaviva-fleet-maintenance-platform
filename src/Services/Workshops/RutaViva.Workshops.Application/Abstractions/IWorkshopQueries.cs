using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.Abstractions;

public interface IWorkshopQueries
{
    Task<IReadOnlyList<WorkshopSummaryDto>> ListAsync(WorkshopStatus? status, string? city, CancellationToken cancellationToken);

    Task<WorkshopDetailDto?> GetDetailAsync(WorkshopId id, CancellationToken cancellationToken);
}
