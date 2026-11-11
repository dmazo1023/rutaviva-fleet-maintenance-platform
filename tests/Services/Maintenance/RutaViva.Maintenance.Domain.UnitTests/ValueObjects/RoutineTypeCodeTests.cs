using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class RoutineTypeCodeTests
{
    private const string InvalidCode = "RoutineTypeCode.Invalid";
    private const string LowerWithSpaces = "  oil_change ";
    private const string Normalized = "OIL_CHANGE";
    private const string Blank = "   ";
    private const string StartsWithDigit = "1OIL";
    private const string ContainsDash = "OIL-CHANGE";
    private const string TooLong = "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDE";

    [Fact]
    public void Create_Should_NormalizeToUpperTrimmed_When_InputIsLowerWithSpaces()
    {
        Result<RoutineTypeCode> result = RoutineTypeCode.Create(LowerWithSpaces);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(StartsWithDigit)]
    [InlineData(ContainsDash)]
    [InlineData(TooLong)]
    public void Create_Should_ReturnInvalidCodeError_When_InputDoesNotMatchPattern(string? value)
    {
        Result<RoutineTypeCode> result = RoutineTypeCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_CodesNormalizeToTheSameValue()
    {
        RoutineTypeCode first = RoutineTypeCode.Create(LowerWithSpaces).Value;
        RoutineTypeCode second = RoutineTypeCode.Create(Normalized).Value;

        first.Should().Be(second);
    }
}
