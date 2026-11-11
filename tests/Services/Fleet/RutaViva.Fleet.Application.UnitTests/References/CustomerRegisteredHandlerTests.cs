using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.UnitTests.References;

public sealed class CustomerRegisteredHandlerTests
{
    private readonly IFleetReferenceWriter _writer = Substitute.For<IFleetReferenceWriter>();
    private readonly IUnitOfWork _unitOfWork = ReferenceEventFactory.SucceedingUnitOfWork();
    private readonly List<CustomerReference> _upserted = [];
    private readonly CustomerRegisteredHandler _handler;

    public CustomerRegisteredHandlerTests()
    {
        _writer.UpsertCustomerAsync(Arg.Do<CustomerReference>(_upserted.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _handler = new CustomerRegisteredHandler(_writer, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_UpsertFullState_When_CustomerRegistered()
    {
        Guid customerId = Guid.NewGuid();
        CustomerRegisteredV1 integrationEvent = Event(customerId, ReferenceEventFactory.ActiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _upserted.Should().ContainSingle();
        CustomerReference reference = _upserted[0];
        reference.Id.Value.Should().Be(customerId);
        reference.TaxId.Should().Be(ReferenceEventFactory.TaxIdValue);
        reference.Name.Should().Be(ReferenceEventFactory.NameValue);
        reference.Status.Should().Be(ReferenceStatus.Active);
        reference.UpdatedAt.Should().Be(ReferenceEventFactory.Instant());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_LeaveSameState_When_SameEventIsAppliedTwice()
    {
        CustomerRegisteredV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.ActiveStatus);

        await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);
        await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        _upserted.Should().HaveCount(2);
        _upserted[1].Should().BeEquivalentTo(_upserted[0]);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatusWithoutUpsert_When_StatusIsUnknown()
    {
        CustomerRegisteredV1 integrationEvent = Event(Guid.NewGuid(), ReferenceEventFactory.UnknownStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ReferenceErrors.InvalidStatus);
        _upserted.Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailureWithoutUpsert_When_CustomerIdIsEmpty()
    {
        CustomerRegisteredV1 integrationEvent = Event(Guid.Empty, ReferenceEventFactory.ActiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.IdentifierEmpty);
        _upserted.Should().BeEmpty();
    }

    private static CustomerRegisteredV1 Event(Guid customerId, string status) =>
        new(
            customerId,
            ReferenceEventFactory.TaxIdValue,
            ReferenceEventFactory.NameValue,
            ReferenceEventFactory.CustomerEmail,
            status,
            ReferenceEventFactory.Instant());
}
