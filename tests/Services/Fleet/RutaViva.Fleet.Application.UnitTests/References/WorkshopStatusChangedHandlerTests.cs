using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Workshops;

namespace RutaViva.Fleet.Application.UnitTests.References;

public sealed class WorkshopStatusChangedHandlerTests
{
    private readonly IFleetReferenceReader _reader = Substitute.For<IFleetReferenceReader>();
    private readonly IFleetReferenceWriter _writer = Substitute.For<IFleetReferenceWriter>();
    private readonly IUnitOfWork _unitOfWork = ReferenceEventFactory.SucceedingUnitOfWork();
    private readonly CapturingLogger<WorkshopStatusChangedHandler> _logger = new();
    private readonly WorkshopStatusChangedHandler _handler;

    public WorkshopStatusChangedHandlerTests()
    {
        _handler = new WorkshopStatusChangedHandler(_reader, _writer, _unitOfWork, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_IgnoreAndSucceed_When_WorkshopIsUnknown()
    {
        WorkshopStatusChangedV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.InactiveStatus);
        _reader.FindWorkshopAsync(Arg.Any<WorkshopId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<WorkshopReference?>(null));

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _writer.DidNotReceive().UpsertWorkshopAsync(Arg.Any<WorkshopReference>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _logger.Entries.Should().ContainSingle(entry => entry.Message.Contains(ReferenceEventFactory.IgnoredLogMarker));
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateOnlyStatusAndTimestamp_When_WorkshopIsKnown()
    {
        Guid id = Guid.NewGuid();
        WorkshopReference existing = Existing(id);
        _reader.FindWorkshopAsync(new WorkshopId(id), Arg.Any<CancellationToken>()).Returns(Task.FromResult<WorkshopReference?>(existing));
        WorkshopStatusChangedV1 integrationEvent = Event(id, ReferenceEventFactory.InactiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        existing.Status.Should().Be(ReferenceStatus.Inactive);
        existing.UpdatedAt.Should().Be(ReferenceEventFactory.LaterInstant());
        existing.Name.Should().Be(ReferenceEventFactory.NameValue);
        await _writer.Received(1).UpsertWorkshopAsync(existing, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatusWithoutLookup_When_StatusIsUnknown()
    {
        WorkshopStatusChangedV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.UnknownStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ReferenceErrors.InvalidStatus);
        await _reader.DidNotReceive().FindWorkshopAsync(Arg.Any<WorkshopId>(), Arg.Any<CancellationToken>());
    }

    private static WorkshopStatusChangedV1 Event(Guid id, string status) =>
        new(id, status, ReferenceEventFactory.LaterInstant());

    private static WorkshopReference Existing(Guid id) =>
        WorkshopReference.Create(new WorkshopId(id), ReferenceEventFactory.TaxIdValue, ReferenceEventFactory.NameValue, ReferenceStatus.Active, ReferenceEventFactory.Instant());
}
