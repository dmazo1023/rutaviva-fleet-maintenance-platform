using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.Workshops;

public sealed class ChangeWorkshopStatusHandler(IWorkshopRepository workshops, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeWorkshopStatusCommand, WorkshopId>
{
    private static readonly IReadOnlyDictionary<WorkshopStatus, Func<Workshop, Result>> Transitions =
        new Dictionary<WorkshopStatus, Func<Workshop, Result>>
        {
            [WorkshopStatus.Active] = workshop => workshop.Activate(),
            [WorkshopStatus.Inactive] = workshop => workshop.Deactivate()
        };

    public async Task<Result<WorkshopId>> HandleAsync(ChangeWorkshopStatusCommand command, CancellationToken cancellationToken)
    {
        Workshop? workshop = await workshops.GetByIdAsync(command.WorkshopId, cancellationToken);
        if (workshop is null)
        {
            return WorkshopErrors.NotFound;
        }

        Result changed = Transitions[command.TargetStatus](workshop);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.WorkshopId;
    }
}
