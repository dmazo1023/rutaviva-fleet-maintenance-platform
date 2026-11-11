using FluentValidation.Results;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class RegisterVehicleCommandValidatorTests
{
    private const string MalformedPlate = "RV-107";
    private const string ShortVin = "9GAJM52348B";
    private const string LowercasePlate = " eva30d ";
    private const string EmptyText = "";
    private const string ModelYearProperty = nameof(RegisterVehicleCommand.ModelYear);
    private const int AncientYear = 1890;
    private const int FarFutureYear = 2100;
    private const long NegativeOdometer = -500;

    private readonly RegisterVehicleCommandValidator _validator = new(new FakeClock(VehicleTestFactory.Now));

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        RegisterVehicleCommand command = ValidCommand();

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_PlateIsLowercaseWithSpaces()
    {
        RegisterVehicleCommand command = ValidCommand() with { Plate = LowercasePlate };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_VinIsNotProvided()
    {
        RegisterVehicleCommand command = ValidCommand() with { Vin = null };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_FailWithInvalidPlate_When_PlateIsMalformed()
    {
        RegisterVehicleCommand command = ValidCommand() with { Plate = MalformedPlate };

        ValidationResult result = _validator.Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(VehicleErrors.InvalidPlate.Code);
    }

    [Fact]
    public void Validate_Should_FailWithInvalidVin_When_VinIsTooShort()
    {
        RegisterVehicleCommand command = ValidCommand() with { Vin = ShortVin };

        ValidationResult result = _validator.Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(VehicleErrors.InvalidVin.Code);
    }

    [Theory]
    [InlineData(AncientYear)]
    [InlineData(FarFutureYear)]
    public void Validate_Should_FailOnModelYear_When_YearIsOutOfRange(int modelYear)
    {
        RegisterVehicleCommand command = ValidCommand() with { ModelYear = modelYear };

        ValidationResult result = _validator.Validate(command);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(ModelYearProperty);
    }

    [Fact]
    public void Validate_Should_FailWithInvalidOdometer_When_OdometerIsNegative()
    {
        RegisterVehicleCommand command = ValidCommand() with { InitialOdometerKm = NegativeOdometer };

        ValidationResult result = _validator.Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(VehicleErrors.InvalidOdometer.Code);
    }

    [Fact]
    public void Validate_Should_FailWithBrandRequired_When_BrandIsEmpty()
    {
        RegisterVehicleCommand command = ValidCommand() with { Brand = EmptyText };

        ValidationResult result = _validator.Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(VehicleErrors.BrandRequired.Code);
    }

    [Fact]
    public void Validate_Should_Fail_When_OwnerIdentifierIsEmpty()
    {
        RegisterVehicleCommand command = ValidCommand() with { CustomerId = default };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    private static RegisterVehicleCommand ValidCommand() =>
        VehicleTestFactory.Command(CustomerId.New(), WorkshopId.New(), ApproverId.New());
}
