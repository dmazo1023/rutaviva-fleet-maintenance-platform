using Microsoft.EntityFrameworkCore;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Infrastructure.Persistence.Repositories;

public sealed class WorkshopRepository(WorkshopsDbContext context) : IWorkshopRepository
{
    public async Task<Workshop?> GetByIdAsync(WorkshopId id, CancellationToken cancellationToken) =>
        await context.Workshops.FirstOrDefaultAsync(workshop => workshop.Id == id, cancellationToken);

    public async Task<Workshop?> GetByTaxIdAsync(TaxId taxId, CancellationToken cancellationToken) =>
        await context.Workshops.FirstOrDefaultAsync(workshop => workshop.TaxId == taxId, cancellationToken);

    public void Add(Workshop workshop) => context.Workshops.Add(workshop);
}
