using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Infrastructure.Persistence.Converters;

public sealed class PhoneNumberConverter() : ValueConverter<PhoneNumber, string>(phone => phone.Value, value => PhoneNumber.Create(value).Value);
