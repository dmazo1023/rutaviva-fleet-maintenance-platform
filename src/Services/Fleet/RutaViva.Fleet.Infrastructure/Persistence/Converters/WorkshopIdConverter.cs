using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class WorkshopIdConverter() : ValueConverter<WorkshopId, Guid>(id => id.Value, value => new WorkshopId(value));
