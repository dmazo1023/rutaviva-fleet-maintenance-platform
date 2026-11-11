using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class EvaluationContextTests
{
    private const string NegativeCode = "EvaluationContext.KilometersNegative";
    private const long OdometerKm = 70100;
    private const long NegativeKm = -1;

    private static readonly DateOnly EffectiveDate = new(2026, 11, 12);

    [Fact]
    public void Create_Should_ReturnContext_When_KilometersAreValid()
    {
        Result<EvaluationContext> result = EvaluationContext.Create(OdometerKm, EffectiveDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.OdometerKm.Should().Be(OdometerKm);
        result.Value.EffectiveDate.Should().Be(EffectiveDate);
    }

    [Fact]
    public void Create_Should_ReturnNegativeKilometersError_When_KilometersAreNegative()
    {
        Result<EvaluationContext> result = EvaluationContext.Create(NegativeKm, EffectiveDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NegativeCode);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_ValuesAreEqual()
    {
        EvaluationContext first = EvaluationContext.Create(OdometerKm, EffectiveDate).Value;
        EvaluationContext second = EvaluationContext.Create(OdometerKm, EffectiveDate).Value;

        first.Should().Be(second);
    }
}
