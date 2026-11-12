using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class ApproverIdConverter() : ValueConverter<ApproverId, Guid>(id => id.Value, value => new ApproverId(value));
