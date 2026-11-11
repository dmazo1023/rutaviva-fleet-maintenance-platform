using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IUnitOfWorkScope : IUnitOfWork, IAsyncDisposable
{
    IMaintenancePlanRepository Plans { get; }

    IRoutineTypeRepository RoutineTypes { get; }

    IAlertRepository Alerts { get; }
}
