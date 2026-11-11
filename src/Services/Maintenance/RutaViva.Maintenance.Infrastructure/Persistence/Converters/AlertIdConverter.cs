using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class AlertIdConverter() : ValueConverter<AlertId, Guid>(id => id.Value, value => new AlertId(value));
