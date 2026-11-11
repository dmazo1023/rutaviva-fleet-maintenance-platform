using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Maintenance.Application.Routines.Bulk;

internal static class BulkRowResults
{
    public static BulkRowResult Valid(int rowNumber) => new(rowNumber, BulkRowOutcome.Valid, []);

    public static BulkRowResult Skipped(int rowNumber) => new(rowNumber, BulkRowOutcome.Skipped, []);

    public static BulkRowResult Invalid(int rowNumber, Error error) =>
        new(rowNumber, BulkRowOutcome.Invalid, [new BulkRowError(error.Code, error.Message)]);

    public static BulkRowResult Invalid(int rowNumber, IReadOnlyList<BulkRowError> errors) =>
        new(rowNumber, BulkRowOutcome.Invalid, errors);
}
