using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Infrastructure.Persistence.Converters;

public sealed class WorkshopIdConverter() : ValueConverter<WorkshopId, Guid>(id => id.Value, value => new WorkshopId(value));
