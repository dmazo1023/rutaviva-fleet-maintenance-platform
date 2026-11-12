using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.Testing.Fixtures;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Infrastructure.Persistence;

namespace RutaViva.Workshops.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class WorkshopsUnitOfWorkTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string OutboxTaxId = "900200001";
    private const string DuplicateTaxId = "900200002";
    private const string StatusTaxId = "900200003";
    private const string OutboxCity = "Medellin";
    private const string DuplicateCity = "Bogota";
    private const string StatusCity = "Cali";

    [Fact]
    public async Task SaveChangesAsync_Should_WriteWorkshopRegisteredToOutbox_When_WorkshopIsRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Workshop workshop = WorkshopTestData.Create(OutboxTaxId, OutboxCity);
        string subject = workshop.Id.Value.ToString();
        await using WorkshopsPersistenceHost host = await WorkshopsPersistenceHost.CreateAsync(database, cancellationToken);
        await using AsyncServiceScope scope = host.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWorkshopRepository>().Add(workshop);

        Result result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        await using WorkshopsDbContext reader = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        OutboxMessage message = await reader.Set<OutboxMessage>().SingleAsync(item => item.Subject == subject, cancellationToken);
        message.Stream.Should().Be(Streams.WorkshopsWorkshop);
        message.EventType.Should().Contain(WorkshopRegisteredV1.EventName);
        (await reader.Workshops.AnyAsync(item => item.Id == workshop.Id, cancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnCatalogError_When_TaxIdIsDuplicated()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsPersistenceHost host = await WorkshopsPersistenceHost.CreateAsync(database, cancellationToken);
        await using AsyncServiceScope firstScope = host.CreateScope();
        firstScope.ServiceProvider.GetRequiredService<IWorkshopRepository>().Add(WorkshopTestData.Create(DuplicateTaxId, DuplicateCity));
        Result first = await firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        await using AsyncServiceScope secondScope = host.CreateScope();
        secondScope.ServiceProvider.GetRequiredService<IWorkshopRepository>().Add(WorkshopTestData.Create(DuplicateTaxId, DuplicateCity));

        Result second = await secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Should().Be(WorkshopErrors.TaxIdAlreadyRegistered);
        secondScope.ServiceProvider.GetRequiredService<WorkshopsDbContext>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_PersistStatusAsTextWithCheck_When_WorkshopIsDeactivated()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Workshop workshop = WorkshopTestData.Create(StatusTaxId, StatusCity);
        await using WorkshopsPersistenceHost host = await WorkshopsPersistenceHost.CreateAsync(database, cancellationToken);
        await using AsyncServiceScope scope = host.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWorkshopRepository>().Add(workshop);
        workshop.Deactivate();

        Result result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        await using WorkshopsDbContext reader = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        Workshop stored = await reader.Workshops.SingleAsync(item => item.Id == workshop.Id, cancellationToken);
        stored.Status.Should().Be(WorkshopStatus.Inactive);
        stored.Address.City.Should().Be(StatusCity);
    }
}
