using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Application.References;

public interface IFleetReferenceReader
{
    Task<CustomerReference?> FindCustomerAsync(CustomerId id, CancellationToken cancellationToken);

    Task<CustomerReference?> FindCustomerByTaxIdAsync(string taxId, CancellationToken cancellationToken);

    Task<WorkshopReference?> FindWorkshopAsync(WorkshopId id, CancellationToken cancellationToken);

    Task<WorkshopReference?> FindWorkshopByTaxIdAsync(string taxId, CancellationToken cancellationToken);

    Task<ApproverReference?> FindApproverAsync(ApproverId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApproverReference>> FindApproversByEmailAsync(string email, CancellationToken cancellationToken);
}
