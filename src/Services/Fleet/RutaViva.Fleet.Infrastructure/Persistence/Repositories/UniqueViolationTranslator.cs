using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Infrastructure.Persistence.Configurations;

namespace RutaViva.Fleet.Infrastructure.Persistence.Repositories;

internal static class UniqueViolationTranslator
{
    private const int DuplicateIndexErrorNumber = 2601;
    private const int DuplicateConstraintErrorNumber = 2627;

    public static bool TryTranslate(DbUpdateException exception, [NotNullWhen(true)] out Error? error)
    {
        error = null;
        if (exception.InnerException is not SqlException sqlException)
        {
            return false;
        }

        if (sqlException.Number is not (DuplicateIndexErrorNumber or DuplicateConstraintErrorNumber))
        {
            return false;
        }

        error = MapIndex(sqlException.Message);
        return error is not null;
    }

    private static Error? MapIndex(string message)
    {
        if (message.Contains(VehicleConfiguration.PlateIndexName, StringComparison.Ordinal))
        {
            return VehicleErrors.PlateAlreadyRegistered;
        }

        if (message.Contains(VehicleConfiguration.VinIndexName, StringComparison.Ordinal))
        {
            return VehicleErrors.VinAlreadyRegistered;
        }

        return null;
    }
}
