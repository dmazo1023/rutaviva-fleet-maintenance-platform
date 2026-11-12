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

    [Fact]
    public void Quote_Should_AutoApprove_When_AmountIsWithinThreshold()
    {
        ServiceOrder order = CreateOrder();

        Result quoted = order.Quote(LowAmount, Policy(), Workshop);

        quoted.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Approved);
        order.AutoApproved.Should().BeTrue();
    }

    [Fact]
    public void Complete_Should_RaiseServiceOrderCompleted_When_HighQuoteIsApprovedAndStarted()
    {
        ServiceOrder order = CreateOrder();

        order.Quote(HighAmount, Policy(), Workshop).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.PendingApproval);
        order.Approve(Approver).IsSuccess.Should().BeTrue();
        order.Start(Workshop).IsSuccess.Should().BeTrue();
        Result completed = order.Complete(ServicedKm, Workshop);

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
        order.Quote(HighAmount, Policy(), Workshop);

        Result rejected = order.Reject(Approver);

        rejected.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Cancelled);
    }

    [Fact]
    public void Complete_Should_Fail_When_OrderWasNotStarted()
    {
        ServiceOrder order = CreateOrder();

        Result completed = order.Complete(ServicedKm, Workshop);

        completed.IsFailure.Should().BeTrue();
        completed.Error.Should().Be(ServiceOrderErrors.InvalidTransition);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_Should_CancelOrder_When_OrderIsApproved()
    {
        ServiceOrder order = CreateOrder();
        order.Quote(LowAmount, Policy(), Workshop);

        Result cancelled = order.Cancel(Operator);

        cancelled.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Fail_When_OrderIsInProgress()
    {
        ServiceOrder order = CreateOrder();
        order.Quote(LowAmount, Policy(), Workshop);
        order.Start(Workshop);

        Result cancelled = order.Cancel(Operator);

        cancelled.IsFailure.Should().BeTrue();
        cancelled.Error.Should().Be(ServiceOrderErrors.InvalidTransition);
    }

    private static QuoteApprovalPolicy Policy() => QuoteApprovalPolicy.Create(Threshold).Value;

    private static ServiceOrder CreateOrder()
    {
        ServiceOrderReference reference = new(
            Guid.CreateVersion7(),
            "RVA104",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Workshop,
            Guid.CreateVersion7(),
            "Frenos",
            "BRAKES");
        return ServiceOrder.Create(reference, Operator).Value;
    }
}
