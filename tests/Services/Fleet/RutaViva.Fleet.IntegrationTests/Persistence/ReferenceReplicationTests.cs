using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class ReferenceReplicationTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const int ExpectedSingleRow = 1;

    [Fact]
    public async Task HandleAsync_Should_KeepSingleCustomerRow_When_RegisteredEventIsHandledTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid customerId = Guid.NewGuid();
        CustomerRegisteredV1 integrationEvent = new(customerId, FleetTestData.NextTaxId(), FleetTestData.NameText, FleetTestData.EmailText, FleetTestData.ActiveStatus, FleetTestData.Instant());

        Result first = await HandleCustomerRegisteredAsync(integrationEvent, cancellationToken);
        Result second = await HandleCustomerRegisteredAsync(integrationEvent, cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        List<CustomerReference> rows = await ReadCustomersAsync(customerId, cancellationToken);
        rows.Should().HaveCount(ExpectedSingleRow);
        rows[0].Status.Should().Be(ReferenceStatus.Active);
        rows[0].TaxId.Should().Be(integrationEvent.TaxId);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateCustomerStatus_When_StatusChangedEventArrives()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid customerId = Guid.NewGuid();
        CustomerRegisteredV1 registered = new(customerId, FleetTestData.NextTaxId(), FleetTestData.NameText, FleetTestData.EmailText, FleetTestData.ActiveStatus, FleetTestData.Instant());
        CustomerStatusChangedV1 changed = new(customerId, FleetTestData.InactiveStatus, FleetTestData.LaterInstant());
        await HandleCustomerRegisteredAsync(registered, cancellationToken);

        Result result = await HandleCustomerStatusChangedAsync(changed, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        List<CustomerReference> rows = await ReadCustomersAsync(customerId, cancellationToken);
        rows.Should().ContainSingle();
        rows[0].Status.Should().Be(ReferenceStatus.Inactive);
        rows[0].UpdatedAt.Should().Be(changed.OccurredAt);
        rows[0].Name.Should().Be(FleetTestData.NameText);
    }

    [Fact]
    public async Task HandleAsync_Should_KeepSingleWorkshopRow_When_RegisteredEventIsHandledTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid workshopId = Guid.NewGuid();
        WorkshopRegisteredV1 integrationEvent = new(workshopId, FleetTestData.NextTaxId(), FleetTestData.NameText, FleetTestData.CityText, FleetTestData.ActiveStatus, FleetTestData.Instant());

        await HandleWorkshopRegisteredAsync(integrationEvent, cancellationToken);
        await HandleWorkshopRegisteredAsync(integrationEvent, cancellationToken);

        List<WorkshopReference> rows = await ReadWorkshopsAsync(workshopId, cancellationToken);
        rows.Should().HaveCount(ExpectedSingleRow);
        rows[0].Status.Should().Be(ReferenceStatus.Active);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateWorkshopStatus_When_StatusChangedEventArrives()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid workshopId = Guid.NewGuid();
        WorkshopRegisteredV1 registered = new(workshopId, FleetTestData.NextTaxId(), FleetTestData.NameText, FleetTestData.CityText, FleetTestData.ActiveStatus, FleetTestData.Instant());
        WorkshopStatusChangedV1 changed = new(workshopId, FleetTestData.InactiveStatus, FleetTestData.LaterInstant());
        await HandleWorkshopRegisteredAsync(registered, cancellationToken);

        Result result = await HandleWorkshopStatusChangedAsync(changed, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        List<WorkshopReference> rows = await ReadWorkshopsAsync(workshopId, cancellationToken);
        rows.Should().ContainSingle();
        rows[0].Status.Should().Be(ReferenceStatus.Inactive);
    }

    [Fact]
    public async Task HandleAsync_Should_KeepSingleApproverRow_When_RegisteredEventIsHandledTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid approverId = Guid.NewGuid();
        ApproverRegisteredV1 integrationEvent = new(approverId, Guid.NewGuid(), FleetTestData.NameText, FleetTestData.EmailText, FleetTestData.ActiveStatus, FleetTestData.Instant());

        await HandleApproverRegisteredAsync(integrationEvent, cancellationToken);
        await HandleApproverRegisteredAsync(integrationEvent, cancellationToken);

        List<ApproverReference> rows = await ReadApproversAsync(approverId, cancellationToken);
        rows.Should().HaveCount(ExpectedSingleRow);
        rows[0].Email.Should().Be(FleetTestData.EmailText);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateApproverStatus_When_StatusChangedEventArrives()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid approverId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        ApproverRegisteredV1 registered = new(approverId, customerId, FleetTestData.NameText, FleetTestData.EmailText, FleetTestData.ActiveStatus, FleetTestData.Instant());
        ApproverStatusChangedV1 changed = new(approverId, customerId, FleetTestData.InactiveStatus, FleetTestData.LaterInstant());
        await HandleApproverRegisteredAsync(registered, cancellationToken);

        Result result = await HandleApproverStatusChangedAsync(changed, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        List<ApproverReference> rows = await ReadApproversAsync(approverId, cancellationToken);
        rows.Should().ContainSingle();
        rows[0].Status.Should().Be(ReferenceStatus.Inactive);
        rows[0].CustomerId.Should().Be(new CustomerId(customerId));
    }

    private Task<Result> HandleCustomerRegisteredAsync(CustomerRegisteredV1 integrationEvent, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new CustomerRegisteredHandler(FleetTestData.WriterOf(context), FleetTestData.UnitOfWorkOf(context))
                .HandleAsync(integrationEvent, FleetTestData.Metadata(), cancellationToken),
            cancellationToken);

    private Task<Result> HandleCustomerStatusChangedAsync(CustomerStatusChangedV1 integrationEvent, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new CustomerStatusChangedHandler(
                    FleetTestData.ReaderOf(context),
                    FleetTestData.WriterOf(context),
                    FleetTestData.UnitOfWorkOf(context),
                    NullLogger<CustomerStatusChangedHandler>.Instance)
                .HandleAsync(integrationEvent, FleetTestData.Metadata(), cancellationToken),
            cancellationToken);

    private Task<Result> HandleWorkshopRegisteredAsync(WorkshopRegisteredV1 integrationEvent, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new WorkshopRegisteredHandler(FleetTestData.WriterOf(context), FleetTestData.UnitOfWorkOf(context))
                .HandleAsync(integrationEvent, FleetTestData.Metadata(), cancellationToken),
            cancellationToken);

    private Task<Result> HandleWorkshopStatusChangedAsync(WorkshopStatusChangedV1 integrationEvent, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new WorkshopStatusChangedHandler(
                    FleetTestData.ReaderOf(context),
                    FleetTestData.WriterOf(context),
                    FleetTestData.UnitOfWorkOf(context),
                    NullLogger<WorkshopStatusChangedHandler>.Instance)
                .HandleAsync(integrationEvent, FleetTestData.Metadata(), cancellationToken),
            cancellationToken);

    private Task<Result> HandleApproverRegisteredAsync(ApproverRegisteredV1 integrationEvent, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new ApproverRegisteredHandler(FleetTestData.WriterOf(context), FleetTestData.UnitOfWorkOf(context))
                .HandleAsync(integrationEvent, FleetTestData.Metadata(), cancellationToken),
            cancellationToken);

    private Task<Result> HandleApproverStatusChangedAsync(ApproverStatusChangedV1 integrationEvent, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => new ApproverStatusChangedHandler(
                    FleetTestData.ReaderOf(context),
                    FleetTestData.WriterOf(context),
                    FleetTestData.UnitOfWorkOf(context),
                    NullLogger<ApproverStatusChangedHandler>.Instance)
                .HandleAsync(integrationEvent, FleetTestData.Metadata(), cancellationToken),
            cancellationToken);

    private Task<List<CustomerReference>> ReadCustomersAsync(Guid id, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => context.CustomerReferences.AsNoTracking().Where(reference => reference.Id == new CustomerId(id)).ToListAsync(cancellationToken),
            cancellationToken);

    private Task<List<WorkshopReference>> ReadWorkshopsAsync(Guid id, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => context.WorkshopReferences.AsNoTracking().Where(reference => reference.Id == new WorkshopId(id)).ToListAsync(cancellationToken),
            cancellationToken);

    private Task<List<ApproverReference>> ReadApproversAsync(Guid id, CancellationToken cancellationToken) =>
        FleetTestData.InContextAsync(
            database,
            context => context.ApproverReferences.AsNoTracking().Where(reference => reference.Id == new ApproverId(id)).ToListAsync(cancellationToken),
            cancellationToken);
}
