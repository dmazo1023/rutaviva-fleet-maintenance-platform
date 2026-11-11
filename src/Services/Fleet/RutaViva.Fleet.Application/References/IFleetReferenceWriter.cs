using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Application.References;

public interface IFleetReferenceWriter
{
    Task UpsertCustomerAsync(CustomerReference reference, CancellationToken cancellationToken);

    Task UpsertWorkshopAsync(WorkshopReference reference, CancellationToken cancellationToken);

    Task UpsertApproverAsync(ApproverReference reference, CancellationToken cancellationToken);
}
