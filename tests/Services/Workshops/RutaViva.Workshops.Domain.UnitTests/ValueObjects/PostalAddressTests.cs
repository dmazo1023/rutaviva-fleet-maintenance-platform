using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain.UnitTests.ValueObjects;

public sealed class PostalAddressTests
{
    private const string AddressRequiredCode = "PostalAddress.AddressRequired";
    private const string AddressTooLongCode = "PostalAddress.AddressTooLong";
    private const string CityRequiredCode = "PostalAddress.CityRequired";
    private const string CityTooLongCode = "PostalAddress.CityTooLong";
    private const string Street = "Cra 50 # 10-20";
    private const string City = "Medellín";
    private const string PaddedStreet = "  Cra 50 # 10-20  ";
    private const string PaddedCity = "  Medellín  ";
    private const string Blank = "   ";
    private const char Filler = 'x';

    [Fact]
    public void Create_Should_ReturnAddress_When_AddressAndCityAreValid()
    {
        Result<PostalAddress> result = PostalAddress.Create(Street, City);

        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(Street);
        result.Value.City.Should().Be(City);
    }

    [Fact]
    public void Create_Should_TrimValues_When_InputIsPadded()
    {
        Result<PostalAddress> result = PostalAddress.Create(PaddedStreet, PaddedCity);

        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(Street);
        result.Value.City.Should().Be(City);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    public void Create_Should_ReturnAddressRequiredError_When_AddressIsBlank(string? address)
    {
        Result<PostalAddress> result = PostalAddress.Create(address, City);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AddressRequiredCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    public void Create_Should_ReturnCityRequiredError_When_CityIsBlank(string? city)
    {
        Result<PostalAddress> result = PostalAddress.Create(Street, city);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CityRequiredCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_Should_ReturnAddressTooLongError_When_AddressExceedsMaxLength()
    {
        string address = new(Filler, PostalAddress.MaxAddressLength + 1);

        Result<PostalAddress> result = PostalAddress.Create(address, City);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AddressTooLongCode);
    }

    [Fact]
    public void Create_Should_ReturnCityTooLongError_When_CityExceedsMaxLength()
    {
        string city = new(Filler, PostalAddress.MaxCityLength + 1);

        Result<PostalAddress> result = PostalAddress.Create(Street, city);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CityTooLongCode);
    }

    [Fact]
    public void Create_Should_Succeed_When_ValuesHaveExactlyTheMaxLength()
    {
        string address = new(Filler, PostalAddress.MaxAddressLength);
        string city = new(Filler, PostalAddress.MaxCityLength);

        Result<PostalAddress> result = PostalAddress.Create(address, city);

        result.IsSuccess.Should().BeTrue();
    }
}
