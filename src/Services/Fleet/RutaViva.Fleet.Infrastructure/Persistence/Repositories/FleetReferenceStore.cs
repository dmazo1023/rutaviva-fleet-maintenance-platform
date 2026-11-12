using Microsoft.EntityFrameworkCore;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Infrastructure.Persistence.Repositories;

public sealed class FleetReferenceStore(FleetDbContext context) : IFleetReferenceReader, IFleetReferenceWriter
{
    public async Task<CustomerReference?> FindCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
        await context.CustomerReferences.AsNoTracking().FirstOrDefaultAsync(reference => reference.Id == id, cancellationToken);

    public async Task<CustomerReference?> FindCustomerByTaxIdAsync(string taxId, CancellationToken cancellationToken) =>
        await context.CustomerReferences.AsNoTracking().FirstOrDefaultAsync(reference => reference.TaxId == taxId, cancellationToken);

    public async Task<WorkshopReference?> FindWorkshopAsync(WorkshopId id, CancellationToken cancellationToken) =>
        await context.WorkshopReferences.AsNoTracking().FirstOrDefaultAsync(reference => reference.Id == id, cancellationToken);

    public async Task<WorkshopReference?> FindWorkshopByTaxIdAsync(string taxId, CancellationToken cancellationToken) =>
        await context.WorkshopReferences.AsNoTracking().FirstOrDefaultAsync(reference => reference.TaxId == taxId, cancellationToken);

    public async Task<ApproverReference?> FindApproverAsync(ApproverId id, CancellationToken cancellationToken) =>
        await context.ApproverReferences.AsNoTracking().FirstOrDefaultAsync(reference => reference.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ApproverReference>> FindApproversByEmailAsync(string email, CancellationToken cancellationToken) =>
        await context.ApproverReferences.AsNoTracking().Where(reference => reference.Email == email).ToListAsync(cancellationToken);

    public async Task UpsertCustomerAsync(CustomerReference reference, CancellationToken cancellationToken)
    {
        CustomerReference? existing = await context.CustomerReferences.FindAsync([reference.Id], cancellationToken);
        if (existing is null)
        {
            context.CustomerReferences.Add(reference);
            return;
        }

        existing.Apply(reference.TaxId, reference.Name, reference.Status, reference.UpdatedAt);
    }

    public async Task UpsertWorkshopAsync(WorkshopReference reference, CancellationToken cancellationToken)
    {
        WorkshopReference? existing = await context.WorkshopReferences.FindAsync([reference.Id], cancellationToken);
        if (existing is null)
        {
            context.WorkshopReferences.Add(reference);
            return;
        }

        existing.Apply(reference.TaxId, reference.Name, reference.Status, reference.UpdatedAt);
    }

    public async Task UpsertApproverAsync(ApproverReference reference, CancellationToken cancellationToken)
    {
        ApproverReference? existing = await context.ApproverReferences.FindAsync([reference.Id], cancellationToken);
        if (existing is null)
        {
            context.ApproverReferences.Add(reference);
            return;
        }

        existing.Apply(reference.CustomerId, reference.FullName, reference.Email, reference.Status, reference.UpdatedAt);
    }
}
