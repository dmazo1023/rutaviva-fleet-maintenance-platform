using FluentValidation;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Application.Bulk;

public sealed class RegisterCustomersBulkHandler(
    ICustomerRepository customers,
    ICustomerUniquenessChecker uniqueness,
    IUnitOfWork unitOfWork,
    IClock clock,
    IEnumerable<IValidator<RegisterCustomerCommand>> validators)
    : ICommandHandler<RegisterCustomersBulkCommand, IReadOnlyList<BulkRowResult>>
{
    public async Task<Result<IReadOnlyList<BulkRowResult>>> HandleAsync(
        RegisterCustomersBulkCommand command,
        CancellationToken cancellationToken)
    {
        CustomerBulkRegistrationHandler bulk = new(BuildRegisterHandler(command.DryRun), uniqueness);
        IReadOnlyList<BulkRowResult> results = await bulk.ExecuteAsync(command.Rows, cancellationToken);
        return Result<IReadOnlyList<BulkRowResult>>.Success(results);
    }

    private ValidationCommandHandlerDecorator<RegisterCustomerCommand, Guid> BuildRegisterHandler(bool dryRun)
    {
        IUnitOfWork scopedUnitOfWork = BulkExecutionScope.For(dryRun).Resolve(unitOfWork);
        RegisterCustomerHandler register = new(customers, uniqueness, scopedUnitOfWork, clock);
        return new ValidationCommandHandlerDecorator<RegisterCustomerCommand, Guid>(register, validators);
    }
}
