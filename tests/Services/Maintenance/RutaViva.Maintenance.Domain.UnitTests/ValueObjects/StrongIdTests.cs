using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class StrongIdTests
{
    private const string EmptyCode = "Identifier.Empty";
    private const string BogotaZoneId = "America/Bogota";

    [Fact]
    public void New_Should_ReturnDistinctNonEmptyIds_When_CalledTwice()
    {
        VehicleId first = VehicleId.New();
        VehicleId second = VehicleId.New();

        first.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
    }

    [Fact]
    public void Create_Should_ReturnEmptyIdentifierError_When_GuidIsEmpty()
    {
        Result<VehicleId> vehicle = VehicleId.Create(Guid.Empty);
        Result<RoutineId> routine = RoutineId.Create(Guid.Empty);
        Result<RoutineTypeId> routineType = RoutineTypeId.Create(Guid.Empty);
        Result<AlertId> alert = AlertId.Create(Guid.Empty);
        Result<CustomerId> customer = CustomerId.Create(Guid.Empty);

        vehicle.Error.Code.Should().Be(EmptyCode);
        routine.Error.Code.Should().Be(EmptyCode);
        routineType.Error.Code.Should().Be(EmptyCode);
        alert.Error.Code.Should().Be(EmptyCode);
        customer.Error.Code.Should().Be(EmptyCode);
    }

    [Fact]
    public void Create_Should_ReturnIdWithSameValue_When_GuidIsNotEmpty()
    {
        Guid value = Guid.NewGuid();

        Result<AlertId> result = AlertId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
        result.Value.Should().Be(new AlertId(value));
    }

    [Fact]
    public void Constructor_Should_Throw_When_GuidIsEmpty()
    {
        Action act = () => _ = new RoutineId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_Should_ReturnGuidText_When_IdIsValid()
    {
        Guid value = Guid.NewGuid();

        new CustomerId(value).ToString().Should().Be(value.ToString());
    }

    [Fact]
    public void MaintenanceTimeZone_Should_UseBogota_When_Accessed()
    {
        MaintenanceTimeZone.Bogota.Id.Should().Be(BogotaZoneId);
    }

    [Fact]
    public void ToLocalDate_Should_ReturnPreviousDay_When_UtcIsAfterMidnightAndBogotaIsBehind()
    {
        DateTimeOffset utcInstant = new(2026, 11, 13, 3, 0, 0, TimeSpan.Zero);

        DateOnly localDate = MaintenanceTimeZone.ToLocalDate(utcInstant);

        localDate.Should().Be(new DateOnly(2026, 11, 12));
    }
}
