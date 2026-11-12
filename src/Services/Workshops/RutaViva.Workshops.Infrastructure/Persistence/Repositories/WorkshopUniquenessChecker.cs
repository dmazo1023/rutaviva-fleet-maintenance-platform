using Microsoft.EntityFrameworkCore;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Infrastructure.Persistence.Repositories;

public sealed class WorkshopUniquenessChecker(WorkshopsDbContext context) : IWorkshopUniquenessChecker
{
    public async Task<bool> IsTaxIdTakenAsync(TaxId taxId, CancellationToken cancellationToken) =>
        await context.Workshops.AsNoTracking().AnyAsync(workshop => workshop.TaxId == taxId, cancellationToken);
}
