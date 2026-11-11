using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.UnitTests;

public sealed class WorkshopErrorsTests
{
    private const string TaxIdAlreadyRegisteredCode = "Workshop.TaxIdAlreadyRegistered";
    private const string NotFoundCode = "Workshop.NotFound";

    [Fact]
    public void TaxIdAlreadyRegistered_Should_BeConflictWithStableCode_When_Read()
    {
        WorkshopErrors.TaxIdAlreadyRegistered.Code.Should().Be(TaxIdAlreadyRegisteredCode);
        WorkshopErrors.TaxIdAlreadyRegistered.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void NotFound_Should_BeNotFoundWithStableCode_When_Read()
    {
        WorkshopErrors.NotFound.Code.Should().Be(NotFoundCode);
        WorkshopErrors.NotFound.Type.Should().Be(ErrorType.NotFound);
    }
}
