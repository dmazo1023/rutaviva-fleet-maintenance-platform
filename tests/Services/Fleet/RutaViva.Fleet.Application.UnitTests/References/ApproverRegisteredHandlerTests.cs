using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.UnitTests.References;

public sealed class ApproverRegisteredHandlerTests
{
    private readonly IFleetReferenceWriter _writer = Substitute.For<IFleetReferenceWriter>();
    private readonly IUnitOfWork _unitOfWork = ReferenceEventFactory.SucceedingUnitOfWork();
    private readonly List<ApproverReference> _upserted = [];
    private readonly ApproverRegisteredHandler _handler;

    public ApproverRegisteredHandlerTests()
    {
        _writer.UpsertApproverAsync(Arg.Do<ApproverReference>(_upserted.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _handler = new ApproverRegisteredHandler(_writer, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_UpsertFullState_When_ApproverRegistered()
    {
        Guid approverId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        ApproverRegisteredV1 integrationEvent = Event(approverId, customerId, ReferenceEventFactory.ActiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _upserted.Should().ContainSingle();
        ApproverReference reference = _upserted[0];
        reference.Id.Value.Should().Be(approverId);
        reference.CustomerId.Value.Should().Be(customerId);
        reference.FullName.Should().Be(ReferenceEventFactory.FullNameValue);
        reference.Email.Should().Be(ReferenceEventFactory.CustomerEmail);
        reference.Status.Should().Be(ReferenceStatus.Active);
        reference.UpdatedAt.Should().Be(ReferenceEventFactory.Instant());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_LeaveSameState_When_SameEventIsAppliedTwice()
    {
        ApproverRegisteredV1 integrationEvent = Event(Guid.NewGuid(), Guid.NewGuid(), ReferenceEventFactory.ActiveStatus);

        await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);
        await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        _upserted.Should().HaveCount(2);
        _upserted[1].Should().BeEquivalentTo(_upserted[0]);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatusWithoutUpsert_When_StatusIsUnknown()
    {
        ApproverRegisteredV1 integrationEvent = Event(Guid.NewGuid(), Guid.NewGuid(), ReferenceEventFactory.UnknownStatus);

        Result result = await _handler.HandleAsync(integrationEvent, ReferenceEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ReferenceErrors.InvalidStatus);
        _upserted.Should().BeEmpty();
    }

    private static ApproverRegisteredV1 Event(Guid approverId, Guid customerId, string status) =>
        new(
            approverId,
            customerId,
            ReferenceEventFactory.FullNameValue,
            ReferenceEventFactory.CustomerEmail,
            status,
            ReferenceEventFactory.Instant());
}
