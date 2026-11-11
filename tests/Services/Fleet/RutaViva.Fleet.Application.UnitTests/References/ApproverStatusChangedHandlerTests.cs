using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.UnitTests.References;

public sealed class ApproverStatusChangedHandlerTests
{
    private readonly IFleetReferenceReader _reader = Substitute.For<IFleetReferenceReader>();
    private readonly IFleetReferenceWriter _writer = Substitute.For<IFleetReferenceWriter>();
    private readonly IUnitOfWork _unitOfWork = ReferenceEventFactory.SucceedingUnitOfWork();
    private readonly CapturingLogger<ApproverStatusChangedHandler> _logger = new();
    private readonly ApproverStatusChangedHandler _handler;

    public ApproverStatusChangedHandlerTests()
    {
        _handler = new ApproverStatusChangedHandler(_reader, _writer, _unitOfWork, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_IgnoreAndSucceed_When_ApproverIsUnknown()
    {
        ApproverStatusChangedV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.InactiveStatus);
        _reader.FindApproverAsync(Arg.Any<ApproverId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ApproverReference?>(null));

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _writer.DidNotReceive().UpsertApproverAsync(Arg.Any<ApproverReference>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _logger.Entries.Should().ContainSingle(entry => entry.Message.Contains(ReferenceEventFactory.IgnoredLogMarker));
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateOnlyStatusAndTimestamp_When_ApproverIsKnown()
    {
        Guid id = Guid.NewGuid();
        ApproverReference existing = Existing(id);
        _reader.FindApproverAsync(new ApproverId(id), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ApproverReference?>(existing));
        ApproverStatusChangedV1 integrationEvent = Event(id, ReferenceEventFactory.InactiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        existing.Status.Should().Be(ReferenceStatus.Inactive);
        existing.UpdatedAt.Should().Be(ReferenceEventFactory.LaterInstant());
        existing.FullName.Should().Be(ReferenceEventFactory.FullNameValue);
        await _writer.Received(1).UpsertApproverAsync(existing, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatusWithoutLookup_When_StatusIsUnknown()
    {
        ApproverStatusChangedV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.UnknownStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ReferenceErrors.InvalidStatus);
        await _reader.DidNotReceive().FindApproverAsync(Arg.Any<ApproverId>(), Arg.Any<CancellationToken>());
    }

    private static ApproverStatusChangedV1 Event(Guid id, string status) =>
        new(id, Guid.NewGuid(), status, ReferenceEventFactory.LaterInstant());

    private static ApproverReference Existing(Guid id) =>
        ApproverReference.Create(new ApproverId(id), CustomerId.New(), ReferenceEventFactory.FullNameValue, ReferenceEventFactory.CustomerEmail, ReferenceStatus.Active, ReferenceEventFactory.Instant());
}
