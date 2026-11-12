using RutaViva.Fleet.Application.Vehicles.Queries;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.UnitTests.Queries;

public sealed class GetVehicleQueryValidatorTests
{
    private const string LowercasePlate = "rva101";
    private const string InvalidPlate = "12";

    private readonly GetVehicleDetailQueryValidator _detailValidator = new();
    private readonly GetVehicleByPlateQueryValidator _plateValidator = new();

    [Fact]
    public void Validate_Should_Pass_When_VehicleIdentifierIsPresent()
    {
        GetVehicleDetailQuery query = new(VehicleId.New());

        bool isValid = _detailValidator.Validate(query).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_VehicleIdentifierIsEmpty()
    {
        GetVehicleDetailQuery query = new(default);

        bool isValid = _detailValidator.Validate(query).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Pass_When_PlateIsLowercaseButWellFormed()
    {
        GetVehicleByPlateQuery query = new(LowercasePlate);

        bool isValid = _plateValidator.Validate(query).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_PlateIsMalformed()
    {
        GetVehicleByPlateQuery query = new(InvalidPlate);

        bool isValid = _plateValidator.Validate(query).IsValid;

        isValid.Should().BeFalse();
    }
}
