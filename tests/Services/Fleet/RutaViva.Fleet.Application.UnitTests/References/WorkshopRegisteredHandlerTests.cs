using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Workshops;

namespace RutaViva.Fleet.Application.UnitTests.References;

public sealed class WorkshopRegisteredHandlerTests
{
    private readonly IFleetReferenceWriter _writer = Substitute.For<IFleetReferenceWriter>();
    private readonly IUnitOfWork _unitOfWork = ReferenceEventFactory.SucceedingUnitOfWork();
    private readonly List<WorkshopReference> _upserted = [];
    private readonly WorkshopRegisteredHandler _handler;

    public WorkshopRegisteredHandlerTests()
    {
        _writer.UpsertWorkshopAsync(Arg.Do<WorkshopReference>(_upserted.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _handler = new WorkshopRegisteredHandler(_writer, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_UpsertFullState_When_WorkshopRegistered()
    {
        Guid workshopId = Guid.NewGuid();
        WorkshopRegisteredV1 integrationEvent = Event(workshopId, ReferenceEventFactory.InactiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _upserted.Should().ContainSingle();
        WorkshopReference reference = _upserted[0];
        reference.Id.Value.Should().Be(workshopId);
        reference.TaxId.Should().Be(ReferenceEventFactory.TaxIdValue);
        reference.Name.Should().Be(ReferenceEventFactory.NameValue);
        reference.Status.Should().Be(ReferenceStatus.Inactive);
        reference.UpdatedAt.Should().Be(ReferenceEventFactory.Instant());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_LeaveSameState_When_SameEventIsAppliedTwice()
    {
        WorkshopRegisteredV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.ActiveStatus);

        await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);
        await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        _upserted.Should().HaveCount(2);
        _upserted[1].Should().BeEquivalentTo(_upserted[0]);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatusWithoutUpsert_When_StatusIsUnknown()
    {
        WorkshopRegisteredV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.UnknownStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ReferenceErrors.InvalidStatus);
        _upserted.Should().BeEmpty();
    }

    private static WorkshopRegisteredV1 Event(Guid workshopId, string status) =>
        new(
            workshopId,
            ReferenceEventFactory.TaxIdValue,
            ReferenceEventFactory.NameValue,
            ReferenceEventFactory.CityValue,
            status,
            ReferenceEventFactory.Instant());
}
