using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Infrastructure.Persistence;

public static class UniqueViolationTranslator
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public static Error? Translate(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
        {
            return null;
        }

        if (!IsUniqueViolation(sqlException))
        {
            return null;
        }

        return ErrorFor(sqlException.Message);
    }

    private static bool IsUniqueViolation(SqlException exception) =>
        exception.Number is UniqueIndexViolation or UniqueConstraintViolation;

    private static Error? ErrorFor(string message)
    {
        if (message.Contains(CustomerIndexNames.TaxId, StringComparison.Ordinal))
        {
            return CustomerErrors.TaxIdAlreadyRegistered;
        }

        if (message.Contains(CustomerIndexNames.ApproverEmail, StringComparison.Ordinal))
        {
            return CustomerErrors.ApproverEmailAlreadyRegistered;
        }

        return null;
    }
}
