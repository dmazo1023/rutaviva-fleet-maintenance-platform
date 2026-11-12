using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Converters;

public sealed class ServiceOrderIdConverter() : ValueConverter<ServiceOrderId, Guid>(id => id.Value, value => new ServiceOrderId(value));
