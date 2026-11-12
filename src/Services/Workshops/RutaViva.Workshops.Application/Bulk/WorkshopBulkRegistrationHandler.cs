using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.Bulk;

public sealed class WorkshopBulkRegistrationHandler(
    ICommandHandler<RegisterWorkshopCommand, Guid> commandHandler,
    IWorkshopRepository workshops)
    : BulkRegistrationHandler<WorkshopBulkRow, RegisterWorkshopCommand>(commandHandler)
{
    protected override int RowNumberOf(WorkshopBulkRow row) => row.RowNumber;

    protected override string NaturalKey(WorkshopBulkRow row) => row.TaxId ?? string.Empty;

    protected override Task<Result<RegisterWorkshopCommand>> ResolveCommandAsync(WorkshopBulkRow row, CancellationToken cancellationToken)
    {
        RegisterWorkshopCommand command = new(row.Name, row.TaxId, row.Email, row.Phone, row.Address, row.City);
        return Task.FromResult(Result<RegisterWorkshopCommand>.Success(command));
    }

    protected override async Task<bool> ExistsAsync(string naturalKey, CancellationToken cancellationToken)
    {
        Result<TaxId> taxId = TaxId.Create(naturalKey);
        if (taxId.IsFailure)
        {
            return false;
        }

        Workshop? existing = await workshops.GetByTaxIdAsync(taxId.Value, cancellationToken);
        return existing is not null;
    }
}
