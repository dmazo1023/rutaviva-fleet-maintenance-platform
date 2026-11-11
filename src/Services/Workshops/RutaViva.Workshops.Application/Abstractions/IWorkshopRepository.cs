using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.Abstractions;

public interface IWorkshopRepository
{
    Task<Workshop?> GetByIdAsync(WorkshopId id, CancellationToken cancellationToken);

    Task<Workshop?> GetByTaxIdAsync(TaxId taxId, CancellationToken cancellationToken);

    void Add(Workshop workshop);
}
