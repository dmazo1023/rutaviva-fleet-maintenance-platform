using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Domain.Common;
using RutaViva.ServiceOrders.Domain.Events;

namespace RutaViva.ServiceOrders.Domain.UnitTests;

public sealed class ServiceOrderTests
{
    private const string Operator = "Operador";
    private const string Workshop = "Taller Norte";
    private const string Approver = "Aprobador";
    private const decimal Threshold = 1_000_000m;
    private const decimal LowAmount = 350_000m;
    private const decimal HighAmount = 1_500_000m;
    private const long ServicedKm = 45_200;

    private static readonly Guid CustomerId = Guid.CreateVersion7();

    private static readonly Guid WorkshopId = Guid.CreateVersion7();

    [Fact]
    public void Quote_Should_AutoApprove_When_AmountIsWithinThreshold()
    {
        ServiceOrder order = CreateOrder();

        Result quoted = order.Quote(LowAmount, Policy(), WorkshopId, Workshop);

        quoted.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Approved);
        order.AutoApproved.Should().BeTrue();
    }

    [Fact]
    public void Complete_Should_RaiseServiceOrderCompleted_When_HighQuoteIsApprovedAndStarted()
    {
        ServiceOrder order = CreateOrder();

        order.Quote(HighAmount, Policy(), WorkshopId, Workshop).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.PendingApproval);
        order.Approve(OwnerApprover()).IsSuccess.Should().BeTrue();
        order.Start(WorkshopId, Workshop).IsSuccess.Should().BeTrue();
        Result completed = order.Complete(ServicedKm, WorkshopId, Workshop);

        completed.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Completed);
        ServiceOrderCompleted raised = order.DomainEvents.OfType<ServiceOrderCompleted>().Single();
        raised.ServicedKm.Should().Be(ServicedKm);
        raised.TotalAmount.Should().Be(HighAmount);
        order.History.Select(entry => entry.Status).Should().Equal(
            ServiceOrderStatus.Created,
            ServiceOrderStatus.Quoted,
            ServiceOrderStatus.PendingApproval,
            ServiceOrderStatus.Approved,
            ServiceOrderStatus.InProgress,
            ServiceOrderStatus.Completed);
    }

    [Fact]
    public void Reject_Should_CancelOrder_When_QuoteIsPendingApproval()
    {
        ServiceOrder order = CreateOrder();
        order.Quote(HighAmount, Policy(), WorkshopId, Workshop);

        Result rejected = order.Reject(OwnerApprover());

        rejected.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Cancelled);
    }

    [Fact]
    public void Complete_Should_Fail_When_OrderWasNotStarted()
    {
        ServiceOrder order = CreateOrder();

        Result completed = order.Complete(ServicedKm, WorkshopId, Workshop);

        completed.IsFailure.Should().BeTrue();
        completed.Error.Should().Be(ServiceOrderErrors.InvalidTransition);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_Should_CancelOrder_When_OrderIsApproved()
    {
        ServiceOrder order = CreateOrder();
        order.Quote(LowAmount, Policy(), WorkshopId, Workshop);

        Result cancelled = order.Cancel(Operator);

        cancelled.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Fail_When_OrderIsInProgress()
    {
        ServiceOrder order = CreateOrder();
        order.Quote(LowAmount, Policy(), WorkshopId, Workshop);
        order.Start(WorkshopId, Workshop);

        Result cancelled = order.Cancel(Operator);

        cancelled.IsFailure.Should().BeTrue();
        cancelled.Error.Should().Be(ServiceOrderErrors.InvalidTransition);
    }

    [Fact]
    public void Approve_Should_Fail_When_ApproverBelongsToAnotherCustomer()
    {
        ServiceOrder order = CreateOrder();
        order.Quote(HighAmount, Policy(), WorkshopId, Workshop);
        ServiceOrderApprover stranger = new(Guid.CreateVersion7(), Guid.CreateVersion7(), Approver, true);

        Result approved = order.Approve(stranger);

        approved.IsFailure.Should().BeTrue();
        approved.Error.Should().Be(ServiceOrderErrors.ApproverNotAuthorized);
        order.Status.Should().Be(ServiceOrderStatus.PendingApproval);
    }

    [Fact]
    public void Quote_Should_Fail_When_WorkshopIsNotAssigned()
    {
        ServiceOrder order = CreateOrder();

        Result quoted = order.Quote(LowAmount, Policy(), Guid.CreateVersion7(), Workshop);

        quoted.IsFailure.Should().BeTrue();
        quoted.Error.Should().Be(ServiceOrderErrors.WorkshopNotAuthorized);
        order.Status.Should().Be(ServiceOrderStatus.Created);
    }

    private static ServiceOrderApprover OwnerApprover() => new(Guid.CreateVersion7(), CustomerId, Approver, true);

    private static QuoteApprovalPolicy Policy() => QuoteApprovalPolicy.Create(Threshold).Value;

    private static ServiceOrder CreateOrder()
    {
        ServiceOrderReference reference = new(
            Guid.CreateVersion7(),
            "RVA104",
            CustomerId,
            WorkshopId,
            Workshop,
            Guid.CreateVersion7(),
            "Frenos",
            "BRAKES",
            null);
        return ServiceOrder.Create(reference, ServiceOrderOrigin.Manual, Operator).Value;
    }
}
