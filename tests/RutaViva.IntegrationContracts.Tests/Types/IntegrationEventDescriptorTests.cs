using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.IntegrationContracts.Tests.Types;

public sealed class IntegrationEventDescriptorTests
{
    private const string ExpectedStream = "fleet.vehicle";
    private const string ExpectedType = "rutaviva.fleet.VehicleRegistered.v1";

    [Fact]
    public void For_Should_ReadConstants_When_EventIsVehicleRegisteredV1()
    {
        IntegrationEventDescriptor descriptor = IntegrationEventDescriptor.For<VehicleRegisteredV1>();

        descriptor.ClrType.Should().Be<VehicleRegisteredV1>();
        descriptor.Stream.Should().Be(ExpectedStream);
        descriptor.EventType.ToString().Should().Be(ExpectedType);
    }

    [Fact]
    public void For_Should_ReturnSameDescriptor_When_CalledWithTypeAndGeneric()
    {
        IntegrationEventDescriptor generic = IntegrationEventDescriptor.For<VehicleRegisteredV1>();
        IntegrationEventDescriptor byType = IntegrationEventDescriptor.For(typeof(VehicleRegisteredV1));

        byType.Should().BeSameAs(generic);
    }

    [Fact]
    public void For_Should_Throw_When_EventDeclaresNoConstants()
    {
        Action act = () => IntegrationEventDescriptor.For<EventWithoutConstants>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void For_Should_Throw_When_TypeIsNotAnIntegrationEvent()
    {
        Action act = () => IntegrationEventDescriptor.For(typeof(string));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsDescribable_Should_ReturnTrue_When_EventDeclaresAllConstants()
    {
        IntegrationEventDescriptor.IsDescribable(typeof(VehicleRegisteredV1)).Should().BeTrue();
    }

    [Fact]
    public void IsDescribable_Should_ReturnFalse_When_EventDeclaresNoConstants()
    {
        IntegrationEventDescriptor.IsDescribable(typeof(EventWithoutConstants)).Should().BeFalse();
    }

    [Fact]
    public void IsDescribable_Should_ReturnFalse_When_TypeIsNotAnIntegrationEvent()
    {
        IntegrationEventDescriptor.IsDescribable(typeof(string)).Should().BeFalse();
    }
}
