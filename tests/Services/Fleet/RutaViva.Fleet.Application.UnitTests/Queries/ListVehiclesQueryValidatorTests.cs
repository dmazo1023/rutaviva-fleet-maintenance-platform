using FluentValidation.Results;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.Vehicles.Queries;

namespace RutaViva.Fleet.Application.UnitTests.Queries;

public sealed class ListVehiclesQueryValidatorTests
{
    private const int BeyondMaximumPageSize = 201;
    private const int BelowMinimumPageSize = 0;
    private const int BelowFirstPage = 0;
    private const int ValidPage = 3;
    private const int ValidPageSize = 25;

    private readonly ListVehiclesQueryValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_DefaultsAreUsed()
    {
        ListVehiclesQuery query = new(null, null, null);

        bool isValid = _validator.Validate(query).IsValid;

        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(VehicleListDefaults.MinimumPageSize)]
    [InlineData(ValidPageSize)]
    [InlineData(VehicleListDefaults.MaximumPageSize)]
    public void Validate_Should_Pass_When_PageSizeIsWithinRange(int pageSize)
    {
        ListVehiclesQuery query = new(null, null, null, ValidPage, pageSize);

        bool isValid = _validator.Validate(query).IsValid;

        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(BeyondMaximumPageSize)]
    [InlineData(BelowMinimumPageSize)]
    public void Validate_Should_ReportValidationError_When_PageSizeIsOutOfRange(int pageSize)
    {
        ListVehiclesQuery query = new(null, null, null, VehicleListDefaults.FirstPage, pageSize);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(failure => failure.ErrorCode == VehicleQueryErrors.InvalidPageSizeCode);
        VehicleQueryErrors.InvalidPageSize.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Validate_Should_ReportValidationError_When_PageIsBelowFirstPage()
    {
        ListVehiclesQuery query = new(null, null, null, BelowFirstPage, ValidPageSize);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(failure => failure.ErrorCode == VehicleQueryErrors.InvalidPageCode);
        VehicleQueryErrors.InvalidPage.Type.Should().Be(ErrorType.Validation);
    }
}
