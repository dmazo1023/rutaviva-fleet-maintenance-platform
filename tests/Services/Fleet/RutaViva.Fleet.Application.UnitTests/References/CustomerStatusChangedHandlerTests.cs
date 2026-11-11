using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.UnitTests.References;

public sealed class CustomerStatusChangedHandlerTests
{
    private readonly IFleetReferenceReader _reader = Substitute.For<IFleetReferenceReader>();
    private readonly IFleetReferenceWriter _writer = Substitute.For<IFleetReferenceWriter>();
    private readonly IUnitOfWork _unitOfWork = ReferenceEventFactory.SucceedingUnitOfWork();
    private readonly CapturingLogger<CustomerStatusChangedHandler> _logger = new();
    private readonly CustomerStatusChangedHandler _handler;

    public CustomerStatusChangedHandlerTests()
    {
        _handler = new CustomerStatusChangedHandler(_reader, _writer, _unitOfWork, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_IgnoreAndSucceed_When_CustomerIsUnknown()
    {
        CustomerStatusChangedV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.InactiveStatus);
        _reader.FindCustomerAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<CustomerReference?>(null));

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _writer.DidNotReceive().UpsertCustomerAsync(Arg.Any<CustomerReference>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _logger.Entries.Should().ContainSingle(entry => entry.Message.Contains(ReferenceEventFactory.IgnoredLogMarker));
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateOnlyStatusAndTimestamp_When_CustomerIsKnown()
    {
        Guid id = Guid.NewGuid();
        CustomerReference existing = Existing(id);
        _reader.FindCustomerAsync(new CustomerId(id), Arg.Any<CancellationToken>()).Returns(Task.FromResult<CustomerReference?>(existing));
        CustomerStatusChangedV1 integrationEvent = Event(id, ReferenceEventFactory.InactiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        existing.Status.Should().Be(ReferenceStatus.Inactive);
        existing.UpdatedAt.Should().Be(ReferenceEventFactory.LaterInstant());
        existing.Name.Should().Be(ReferenceEventFactory.NameValue);
        await _writer.Received(1).UpsertCustomerAsync(existing, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatusWithoutLookup_When_StatusIsUnknown()
    {
        CustomerStatusChangedV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.UnknownStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ReferenceErrors.InvalidStatus);
        await _reader.DidNotReceive().FindCustomerAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>());
    }

    private static CustomerStatusChangedV1 Event(Guid id, string status) =>
        new(id, status, ReferenceEventFactory.LaterInstant());

    private static CustomerReference Existing(Guid id) =>
        CustomerReference.Create(new CustomerId(id), ReferenceEventFactory.TaxIdValue, ReferenceEventFactory.NameValue, ReferenceStatus.Active, ReferenceEventFactory.Instant());
}
