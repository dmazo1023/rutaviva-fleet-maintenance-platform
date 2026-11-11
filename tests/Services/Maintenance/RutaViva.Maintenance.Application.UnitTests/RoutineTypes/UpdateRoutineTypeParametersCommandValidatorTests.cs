using FluentValidation.Results;
using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.RoutineTypes;

public sealed class UpdateRoutineTypeParametersCommandValidatorTests
{
    private const string UpdatedBy = "ops.user";
    private const string EmptyText = "";
    private const string BlankText = "   ";
    private const string UpdatedByRequiredCode = "Audit.UpdatedByRequired";
    private const string IntervalEmptyCode = "MaintenanceInterval.Empty";
    private const string ThresholdOutOfRangeCode = "DueSoonThreshold.OutOfRange";
    private const int IntervalKm = 10000;
    private const int IntervalMonths = 6;
    private const decimal ValidPercent = 10m;

    private readonly UpdateRoutineTypeParametersCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_Should_Pass_When_CommandIsValid()
    {
        ValidationResult result = await _validator.ValidateAsync(CommandWith(IntervalKm, IntervalMonths, ValidPercent, UpdatedBy), TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmptyText)]
    [InlineData(BlankText)]
    public async Task ValidateAsync_Should_Fail_When_UpdatedByIsBlank(string updatedBy)
    {
        ValidationResult result = await _validator.ValidateAsync(CommandWith(IntervalKm, IntervalMonths, ValidPercent, updatedBy), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(UpdatedByRequiredCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-5)]
    [InlineData(150)]
    public async Task ValidateAsync_Should_Fail_When_ThresholdIsOutOfRange(int percent)
    {
        ValidationResult result = await _validator.ValidateAsync(CommandWith(IntervalKm, IntervalMonths, percent, UpdatedBy), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ThresholdOutOfRangeCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task ValidateAsync_Should_Pass_When_ThresholdIsOnTheLimit(int percent)
    {
        ValidationResult result = await _validator.ValidateAsync(CommandWith(IntervalKm, IntervalMonths, percent, UpdatedBy), TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_NoDimensionIsGiven()
    {
        ValidationResult result = await _validator.ValidateAsync(CommandWith(null, null, ValidPercent, UpdatedBy), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(IntervalEmptyCode);
    }

    [Theory]
    [InlineData(IntervalKm, null)]
    [InlineData(null, IntervalMonths)]
    public async Task ValidateAsync_Should_Pass_When_OnlyOneDimensionIsGiven(int? kilometers, int? months)
    {
        ValidationResult result = await _validator.ValidateAsync(CommandWith(kilometers, months, ValidPercent, UpdatedBy), TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    private static UpdateRoutineTypeParametersCommand CommandWith(int? kilometers, int? months, decimal percent, string updatedBy) =>
        new(RoutineTypeId.New(), kilometers, months, percent, updatedBy);
}
