using FluentValidation.Results;
using RutaViva.Maintenance.Application.Telemetry;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

public sealed class IngestTelemetryReadingCommandValidatorTests
{
    private const string ValidPlate = "RVA101";
    private const string Blank = "   ";
    private const long ValidKm = 1000;
    private const long ZeroKm = 0;
    private const long NegativeKm = -5;

    private readonly IngestTelemetryReadingCommandValidator _validator = new();

    public static TheoryData<string> BlankPlates => new() { string.Empty, Blank };

    [Fact]
    public void Validate_Should_BeValid_When_CommandIsComplete()
    {
        ValidationResult result = _validator.Validate(new IngestTelemetryReadingCommand(ValidPlate, ValidKm, TelemetryTestData.ReadAt));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_BeValid_When_KilometersAreZero()
    {
        ValidationResult result = _validator.Validate(new IngestTelemetryReadingCommand(ValidPlate, ZeroKm, TelemetryTestData.ReadAt));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(BlankPlates))]
    public void Validate_Should_Fail_When_PlateIsBlank(string plate)
    {
        ValidationResult result = _validator.Validate(new IngestTelemetryReadingCommand(plate, ValidKm, TelemetryTestData.ReadAt));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(IngestTelemetryReadingCommand.Plate));
    }

    [Fact]
    public void Validate_Should_Fail_When_KilometersAreNegative()
    {
        ValidationResult result = _validator.Validate(new IngestTelemetryReadingCommand(ValidPlate, NegativeKm, TelemetryTestData.ReadAt));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(IngestTelemetryReadingCommand.OdometerKm));
    }

    [Fact]
    public void Validate_Should_Fail_When_ReadAtIsDefault()
    {
        ValidationResult result = _validator.Validate(new IngestTelemetryReadingCommand(ValidPlate, ValidKm, default));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(IngestTelemetryReadingCommand.ReadAt));
    }
}
