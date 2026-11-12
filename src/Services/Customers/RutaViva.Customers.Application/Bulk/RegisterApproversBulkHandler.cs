using FluentValidation;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Approvers;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Application.Bulk;

public sealed class RegisterApproversBulkHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock,
    IEnumerable<IValidator<AddApproverCommand>> validators)
    : ICommandHandler<RegisterApproversBulkCommand, IReadOnlyList<BulkRowResult>>
{
    public async Task<Result<IReadOnlyList<BulkRowResult>>> HandleAsync(
        RegisterApproversBulkCommand command,
        CancellationToken cancellationToken)
    {
        ApproverBulkRegistrationHandler bulk = new(BuildAddHandler(command.DryRun), customers);
        IReadOnlyList<BulkRowResult> results = await bulk.ExecuteAsync(command.Rows, cancellationToken);
        return Result<IReadOnlyList<BulkRowResult>>.Success(results);
    }

    private ValidationCommandHandlerDecorator<AddApproverCommand, Guid> BuildAddHandler(bool dryRun)
    {
        IUnitOfWork scopedUnitOfWork = BulkExecutionScope.For(dryRun).Resolve(unitOfWork);
        AddApproverHandler add = new(customers, scopedUnitOfWork, clock);
        return new ValidationCommandHandlerDecorator<AddApproverCommand, Guid>(add, validators);
    }
}
