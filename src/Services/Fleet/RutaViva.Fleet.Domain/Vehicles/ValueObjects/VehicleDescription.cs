using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.Vehicles.ValueObjects;

public sealed record VehicleDescription : ValueObject
{
    public const int MinimumModelYear = 1950;

    public const int ModelYearsAheadAllowed = 1;

    private VehicleDescription(string brand, string model, int modelYear)
    {
        Brand = brand;
        Model = model;
        ModelYear = modelYear;
    }

    public string Brand { get; }

    public string Model { get; }

    public int ModelYear { get; }

    public static Result<VehicleDescription> Create(string? brand, string? model, int modelYear, int currentYear)
    {
        Error? invalid = ValidateBrand(brand) ?? ValidateModel(model) ?? ValidateModelYear(modelYear, currentYear);
        if (invalid is not null)
        {
            return invalid;
        }

        return new VehicleDescription(brand!.Trim(), model!.Trim(), modelYear);
    }

    private static Error? ValidateBrand(string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            return VehicleErrors.BrandRequired;
        }

        return null;
    }

    private static Error? ValidateModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return VehicleErrors.ModelRequired;
        }

        return null;
    }

    private static Error? ValidateModelYear(int modelYear, int currentYear)
    {
        if (modelYear < MinimumModelYear || modelYear > currentYear + ModelYearsAheadAllowed)
        {
            return VehicleErrors.InvalidModelYear;
        }

        return null;
    }
}
