using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.Abstractions;

public interface IWorkshopUniquenessChecker
{
    Task<bool> IsTaxIdTakenAsync(TaxId taxId, CancellationToken cancellationToken);
}
