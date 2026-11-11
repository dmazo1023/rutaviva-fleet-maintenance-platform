using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class MaintenanceCycleTests
{
    private const string NumberCode = "MaintenanceCycle.NumberInvalid";
    private const string StartKmCode = "MaintenanceCycle.StartKmNegative";
    private const string ServiceCode = "ServiceRecord.KilometersNegative";
    private const int FirstCycle = 1;
    private const int SecondCycle = 2;
    private const long StartKm = 45000;
    private const long ServiceKm = 98400;
    private const long NegativeKm = -1;

    private static readonly DateOnly StartDate = new(2026, 8, 1);
    private static readonly DateOnly ServiceDate = new(2026, 11, 12);

    [Fact]
    public void Start_Should_ReturnFirstCycle_When_ValuesAreValid()
    {
        Result<MaintenanceCycle> result = MaintenanceCycle.Start(StartKm, StartDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be(FirstCycle);
        result.Value.StartKm.Should().Be(StartKm);
        result.Value.StartDate.Should().Be(StartDate);
    }

    [Fact]
    public void Create_Should_ReturnNumberError_When_NumberIsLessThanOne()
    {
        Result<MaintenanceCycle> result = MaintenanceCycle.Create(FirstCycle - 1, StartKm, StartDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NumberCode);
    }

    [Fact]
    public void Create_Should_ReturnStartKmError_When_StartKmIsNegative()
    {
        Result<MaintenanceCycle> result = MaintenanceCycle.Create(FirstCycle, NegativeKm, StartDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StartKmCode);
    }

    [Fact]
    public void Next_Should_IncrementNumberAndStartAtService_When_ServiceIsCompleted()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(StartKm, StartDate).Value;
        ServiceRecord service = ServiceRecord.Create(ServiceKm, ServiceDate).Value;

        MaintenanceCycle next = cycle.Next(service);

        next.Number.Should().Be(SecondCycle);
        next.StartKm.Should().Be(ServiceKm);
        next.StartDate.Should().Be(ServiceDate);
        cycle.Number.Should().Be(FirstCycle);
    }

    [Fact]
    public void ServiceRecordCreate_Should_ReturnKilometersError_When_KilometersAreNegative()
    {
        Result<ServiceRecord> result = ServiceRecord.Create(NegativeKm, ServiceDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ServiceCode);
    }

    [Fact]
    public void ServiceRecordCreate_Should_ReturnRecord_When_ValuesAreValid()
    {
        Result<ServiceRecord> result = ServiceRecord.Create(ServiceKm, ServiceDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kilometers.Should().Be(ServiceKm);
        result.Value.Date.Should().Be(ServiceDate);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_AllComponentsAreEqual()
    {
        MaintenanceCycle first = MaintenanceCycle.Start(StartKm, StartDate).Value;
        MaintenanceCycle second = MaintenanceCycle.Start(StartKm, StartDate).Value;

        first.Should().Be(second);
    }
}
