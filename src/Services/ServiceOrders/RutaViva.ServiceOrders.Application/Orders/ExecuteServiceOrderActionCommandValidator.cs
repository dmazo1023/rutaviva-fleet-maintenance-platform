using FluentValidation;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class ExecuteServiceOrderActionCommandValidator : AbstractValidator<ExecuteServiceOrderActionCommand>
{
    private const string ActionInvalidMessage = "The service order action is not valid.";
    private const string ActorRequiredMessage = "The actor name is required.";
    private const string AmountRequiredMessage = "The quoted amount is required.";
    private const string ServicedKmRequiredMessage = "The serviced kilometers are required.";
    private const string ApproverRequiredMessage = "The approver identifier is required.";
    private const string WorkshopRequiredMessage = "The workshop identifier is required.";

    public ExecuteServiceOrderActionCommandValidator()
    {
        RuleFor(command => command.Action).IsInEnum().WithMessage(ActionInvalidMessage);
        RuleFor(command => command.Actor).NotEmpty().WithMessage(ActorRequiredMessage);
        RuleFor(command => command.Amount)
            .NotNull()
            .When(command => command.Action == ServiceOrderAction.Quote)
            .WithMessage(AmountRequiredMessage);
        RuleFor(command => command.ServicedKm)
            .NotNull()
            .When(command => command.Action == ServiceOrderAction.Complete)
            .WithMessage(ServicedKmRequiredMessage);
        RuleFor(command => command.ApproverId)
            .NotEmpty()
            .When(command => command.Action is ServiceOrderAction.Approve or ServiceOrderAction.Reject)
            .WithMessage(ApproverRequiredMessage);
        RuleFor(command => command.WorkshopId)
            .NotEmpty()
            .When(command => command.Action is ServiceOrderAction.Quote or ServiceOrderAction.Start or ServiceOrderAction.Complete)
            .WithMessage(WorkshopRequiredMessage);
    }
}
