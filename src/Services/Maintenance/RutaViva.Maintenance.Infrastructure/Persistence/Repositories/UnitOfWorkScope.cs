using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

internal sealed class UnitOfWorkScope : IUnitOfWorkScope
{
    private readonly AsyncServiceScope _scope;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkScope(AsyncServiceScope scope)
    {
        _scope = scope;
        _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Plans = scope.ServiceProvider.GetRequiredService<IMaintenancePlanRepository>();
        RoutineTypes = scope.ServiceProvider.GetRequiredService<IRoutineTypeRepository>();
        Alerts = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
    }

    public IMaintenancePlanRepository Plans { get; }

    public IRoutineTypeRepository RoutineTypes { get; }

    public IAlertRepository Alerts { get; }

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) =>
        _unitOfWork.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}
