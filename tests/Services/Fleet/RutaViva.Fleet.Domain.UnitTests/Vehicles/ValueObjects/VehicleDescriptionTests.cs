using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.UnitTests.Vehicles.ValueObjects;

public sealed class VehicleDescriptionTests
{
    private const string BrandRequiredCode = "Vehicle.BrandRequired";
    private const string ModelRequiredCode = "Vehicle.ModelRequired";
    private const string InvalidYearCode = "Vehicle.InvalidModelYear";
    private const string Brand = "Renault";
    private const string PaddedBrand = "  Renault ";
    private const string Model = "Kangoo";
    private const string PaddedModel = " Kangoo  ";
    private const string Blank = "   ";
    private const int CurrentYear = 2026;
    private const int ValidYear = 2022;
    private const int AncientYear = 1890;
    private const int BeforeMinimumYear = 1949;
    private const int NextYearOffset = 1;
    private const int TwoYearsAheadOffset = 2;

    [Fact]
    public void Create_Should_TrimBrandAndModel_When_DataIsValid()
    {
        Result<VehicleDescription> result = VehicleDescription.Create(PaddedBrand, PaddedModel, ValidYear, CurrentYear);

        result.IsSuccess.Should().BeTrue();
        result.Value.Brand.Should().Be(Brand);
        result.Value.Model.Should().Be(Model);
        result.Value.ModelYear.Should().Be(ValidYear);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    public void Create_Should_ReturnBrandRequired_When_BrandIsBlank(string? brand)
    {
        Result<VehicleDescription> result = VehicleDescription.Create(brand, Model, ValidYear, CurrentYear);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(BrandRequiredCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    public void Create_Should_ReturnModelRequired_When_ModelIsBlank(string? model)
    {
        Result<VehicleDescription> result = VehicleDescription.Create(Brand, model, ValidYear, CurrentYear);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ModelRequiredCode);
    }

    [Theory]
    [InlineData(AncientYear)]
    [InlineData(BeforeMinimumYear)]
    [InlineData(CurrentYear + TwoYearsAheadOffset)]
    public void Create_Should_ReturnInvalidYear_When_YearIsOutOfRange(int year)
    {
        Result<VehicleDescription> result = VehicleDescription.Create(Brand, Model, year, CurrentYear);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidYearCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(VehicleDescription.MinimumModelYear)]
    [InlineData(CurrentYear)]
    [InlineData(CurrentYear + NextYearOffset)]
    public void Create_Should_Succeed_When_YearIsOnTheBoundary(int year)
    {
        Result<VehicleDescription> result = VehicleDescription.Create(Brand, Model, year, CurrentYear);

        result.IsSuccess.Should().BeTrue();
    }
}
