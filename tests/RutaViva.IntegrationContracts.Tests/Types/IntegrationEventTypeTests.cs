namespace RutaViva.IntegrationContracts.Tests.Types;

public sealed class IntegrationEventTypeTests
{
    private const string Service = "fleet";
    private const string Name = "VehicleRegistered";
    private const string Stream = "fleet.vehicle";
    private const int Version = 1;
    private const int SecondVersion = 2;
    private const int InvalidVersion = 0;
    private const string Blank = " ";
    private const string ExpectedText = "rutaviva.fleet.VehicleRegistered.v1";
    private const string ExpectedSecondVersionText = "rutaviva.fleet.VehicleRegistered.v2";
    private const string WrongPrefix = "other.fleet.VehicleRegistered.v1";
    private const string MissingSegment = "rutaviva.fleet.VehicleRegistered";
    private const string ExtraSegment = "rutaviva.fleet.VehicleRegistered.v1.extra";
    private const string MissingVersionPrefix = "rutaviva.fleet.VehicleRegistered.1";
    private const string ZeroVersion = "rutaviva.fleet.VehicleRegistered.v0";
    private const string NegativeVersion = "rutaviva.fleet.VehicleRegistered.v-1";
    private const string NonNumericVersion = "rutaviva.fleet.VehicleRegistered.vx";
    private const string EmptyService = "rutaviva..VehicleRegistered.v1";
    private const string BlankName = "rutaviva.fleet. .v1";

    [Fact]
    public void ToString_Should_FormatCanonicalType_When_CreatedFromParts()
    {
        IntegrationEventType eventType = IntegrationEventType.Create(Service, Name, Version);

        eventType.ToString().Should().Be(ExpectedText);
    }

    [Fact]
    public void ToString_Should_IncludeVersion_When_VersionIsGreaterThanOne()
    {
        IntegrationEventType eventType = IntegrationEventType.Create(Service, Name, SecondVersion);

        eventType.ToString().Should().Be(ExpectedSecondVersionText);
    }

    [Fact]
    public void ForStream_Should_UseStreamPrefixAsService_When_StreamHasDot()
    {
        IntegrationEventType eventType = IntegrationEventType.ForStream(Stream, Name, Version);

        eventType.Service.Should().Be(Service);
        eventType.Name.Should().Be(Name);
        eventType.Version.Should().Be(Version);
    }

    [Fact]
    public void Create_Should_Throw_When_VersionIsLessThanOne()
    {
        Action act = () => IntegrationEventType.Create(Service, Name, InvalidVersion);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Should_Throw_When_ServiceIsBlank()
    {
        Action act = () => IntegrationEventType.Create(Blank, Name, Version);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParse_Should_ReturnParts_When_ValueIsValid()
    {
        bool parsed = IntegrationEventType.TryParse(ExpectedText, out IntegrationEventType? eventType);

        parsed.Should().BeTrue();
        eventType.Should().Be(IntegrationEventType.Create(Service, Name, Version));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(WrongPrefix)]
    [InlineData(MissingSegment)]
    [InlineData(ExtraSegment)]
    [InlineData(MissingVersionPrefix)]
    [InlineData(ZeroVersion)]
    [InlineData(NegativeVersion)]
    [InlineData(NonNumericVersion)]
    [InlineData(EmptyService)]
    [InlineData(BlankName)]
    public void TryParse_Should_ReturnFalse_When_ValueIsInvalid(string? value)
    {
        bool parsed = IntegrationEventType.TryParse(value, out IntegrationEventType? eventType);

        parsed.Should().BeFalse();
        eventType.Should().BeNull();
    }
}
