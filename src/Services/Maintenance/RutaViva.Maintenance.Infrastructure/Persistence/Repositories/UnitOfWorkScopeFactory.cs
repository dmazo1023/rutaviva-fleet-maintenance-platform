using Microsoft.Extensions.DependencyInjection;
using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWorkScopeFactory(IServiceScopeFactory scopeFactory) : IUnitOfWorkScopeFactory
{
    public IUnitOfWorkScope Create() => new UnitOfWorkScope(scopeFactory.CreateAsyncScope());
}
