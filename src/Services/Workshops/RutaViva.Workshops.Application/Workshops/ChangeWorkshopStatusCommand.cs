using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.Workshops;

public sealed record ChangeWorkshopStatusCommand(WorkshopId WorkshopId, WorkshopStatus TargetStatus)
    : ICommand<WorkshopId>;
