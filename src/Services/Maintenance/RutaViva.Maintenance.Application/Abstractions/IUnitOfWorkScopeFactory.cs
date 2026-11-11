namespace RutaViva.Maintenance.Application.Abstractions;

public interface IUnitOfWorkScopeFactory
{
    IUnitOfWorkScope Create();
}
