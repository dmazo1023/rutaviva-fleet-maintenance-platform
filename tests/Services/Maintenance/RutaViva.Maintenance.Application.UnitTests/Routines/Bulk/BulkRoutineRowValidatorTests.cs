using FluentValidation.Results;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Application.Routines.Bulk;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Routines.Bulk;

public sealed class BulkRoutineRowValidatorTests
{
    private const string ValidPlate = "RVA101";
    private const string MalformedPlate = "12";
    private const string ValidCode = "OIL_CHANGE";
    private const string MalformedCode = "oil change";
    private const long NegativeKilometers = -1;
    private const long ValidKilometers = 15000;
    private const int RowNumber = 1;

    private readonly BulkRoutineRowValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_RowHasValidFormat()
    {
        ValidationResult result = _validator.Validate(Row(ValidPlate, ValidCode, ValidKilometers));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_ReturnPlateError_When_PlateIsMalformed()
    {
        ValidationResult result = _validator.Validate(Row(MalformedPlate, ValidCode, null));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(MaintenanceErrors.LicensePlateInvalid.Code);
    }

    [Fact]
    public void Validate_Should_ReturnCodeError_When_RoutineTypeCodeIsMalformed()
    {
        ValidationResult result = _validator.Validate(Row(ValidPlate, MalformedCode, null));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(MaintenanceErrors.RoutineTypeCodeInvalid.Code);
    }

    [Fact]
    public void Validate_Should_ReturnKilometersError_When_LastServiceKmIsNegative()
    {
        ValidationResult result = _validator.Validate(Row(ValidPlate, ValidCode, NegativeKilometers));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(MaintenanceErrors.ServiceRecordKilometersNegative.Code);
    }

    private static RoutineBulkRow Row(string plate, string code, long? lastServiceKm) =>
        new(RowNumber, plate, code, lastServiceKm, null, null, null, null);
}
