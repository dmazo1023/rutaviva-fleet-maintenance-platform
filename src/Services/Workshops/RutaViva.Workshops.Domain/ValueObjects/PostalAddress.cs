using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.ValueObjects;

public sealed record PostalAddress : ValueObject
{
    public const int MaxAddressLength = 200;
    public const int MaxCityLength = 100;

    private PostalAddress(string address, string city)
    {
        Address = address;
        City = city;
    }

    public string Address { get; }

    public string City { get; }

    public static Result<PostalAddress> Create(string? address, string? city)
    {
        Result<string> validAddress = Normalize(address, MaxAddressLength, WorkshopErrors.AddressRequired, WorkshopErrors.AddressTooLong);
        if (validAddress.IsFailure)
        {
            return validAddress.Error;
        }

        Result<string> validCity = Normalize(city, MaxCityLength, WorkshopErrors.CityRequired, WorkshopErrors.CityTooLong);
        if (validCity.IsFailure)
        {
            return validCity.Error;
        }

        return new PostalAddress(validAddress.Value, validCity.Value);
    }

    private static Result<string> Normalize(string? value, int maxLength, Error required, Error tooLong)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return required;
        }

        string normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            return tooLong;
        }

        return normalized;
    }
}
