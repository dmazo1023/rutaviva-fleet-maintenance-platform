using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Repositories;

[Trait("Category", "Integration")]
public sealed class UnitOfWorkScopeFactoryTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ScopePlate = "SCP123";

    [Fact]
    public async Task Create_Should_ReturnIndependentScopes_When_CalledTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = await BuildProviderAsync(cancellationToken);
        IUnitOfWorkScopeFactory factory = provider.GetRequiredService<IUnitOfWorkScopeFactory>();

        await using IUnitOfWorkScope first = factory.Create();
        await using IUnitOfWorkScope second = factory.Create();

        first.Should().NotBeSameAs(second);
        first.Plans.Should().NotBeSameAs(second.Plans);
        first.RoutineTypes.Should().NotBeSameAs(second.RoutineTypes);
        first.Alerts.Should().NotBeSameAs(second.Alerts);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_IsolateChanges_When_ScopesAreIndependent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan seeded = await PlanSeeding.SavePlanAsync(database, ScopePlate, cancellationToken);
        await using ServiceProvider provider = await BuildProviderAsync(cancellationToken);
        IUnitOfWorkScopeFactory factory = provider.GetRequiredService<IUnitOfWorkScopeFactory>();
        await using IUnitOfWorkScope writer = factory.Create();
        await using IUnitOfWorkScope reader = factory.Create();
        MaintenancePlan writerPlan = (await writer.Plans.GetAsync(seeded.Id, cancellationToken))!;
        MaintenancePlan readerPlan = (await reader.Plans.GetAsync(seeded.Id, cancellationToken))!;

        writerPlan.Suspend();
        MaintenancePlan beforeSave = (await reader.Plans.GetAsync(seeded.Id, cancellationToken))!;
        await writer.SaveChangesAsync(cancellationToken);
        await using IUnitOfWorkScope fresh = factory.Create();
        MaintenancePlan afterSave = (await fresh.Plans.GetAsync(seeded.Id, cancellationToken))!;

        readerPlan.Should().NotBeSameAs(writerPlan);
        beforeSave.Status.Should().Be(MaintenancePlanStatus.Active);
        afterSave.Status.Should().Be(MaintenancePlanStatus.Suspended);
    }

    private async Task<ServiceProvider> BuildProviderAsync(CancellationToken cancellationToken)
    {
        await using MaintenanceDbContext schemaContext = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        ServiceCollection services = new();
        services.AddDbContext<MaintenanceDbContext>(options => options.UseSqlServer(database.ConnectionString));
        services.AddScoped<IMaintenancePlanRepository, MaintenancePlanRepository>();
        services.AddScoped<IRoutineTypeRepository, RoutineTypeRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUnitOfWork, MaintenanceUnitOfWork>();
        services.AddSingleton<IUnitOfWorkScopeFactory, UnitOfWorkScopeFactory>();
        return services.BuildServiceProvider();
    }
}
