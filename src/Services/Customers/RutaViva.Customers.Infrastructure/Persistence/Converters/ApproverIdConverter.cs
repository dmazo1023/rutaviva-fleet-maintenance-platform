using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Infrastructure.Persistence.Converters;

public sealed class ApproverIdConverter() : ValueConverter<ApproverId, Guid>(id => id.Value, value => new ApproverId(value));
