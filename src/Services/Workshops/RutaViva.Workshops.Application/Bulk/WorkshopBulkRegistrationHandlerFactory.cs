using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;

namespace RutaViva.Workshops.Application.Bulk;

public sealed class WorkshopBulkRegistrationHandlerFactory(
    IWorkshopRepository workshops,
    IWorkshopUniquenessChecker uniqueness,
    IUnitOfWork unitOfWork)
{
    public WorkshopBulkRegistrationHandler Create(bool dryRun)
    {
        IUnitOfWork scopedUnitOfWork = BulkExecutionScope.For(dryRun).Resolve(unitOfWork);
        RegisterWorkshopHandler registration = new(workshops, uniqueness, scopedUnitOfWork);
        return new WorkshopBulkRegistrationHandler(registration, workshops);
    }
}
