using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.Events;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.RoutineTypes;

public sealed class RoutineTypeTests
{
    private const string UpdatedByRequiredCode = "Audit.UpdatedByRequired";
    private const string NameRequiredCode = "RoutineType.NameRequired";
    private const string OilChangeCode = "OIL_CHANGE";
    private const string OilChangeName = "Oil change";
    private const string PaddedName = "  Oil change  ";
    private const string Creator = "seed";
    private const string Editor = "admin@rutaviva.test";
    private const string PaddedEditor = "  admin@rutaviva.test  ";
    private const string Blank = "   ";
    private const int OilChangeKm = 10000;
    private const int OilChangeMonths = 6;
    private const int NewKm = 12000;
    private const int NewMonths = 8;
    private const decimal TenPercent = 10m;
    private const decimal TwentyPercent = 20m;
    private const int CreatedYear = 2026;
    private const int CreatedMonth = 1;
    private const int CreatedDay = 1;
    private const int UpdatedMonth = 11;
    private const int UpdatedDay = 12;

    private static readonly DateTimeOffset CreatedAt = new(CreatedYear, CreatedMonth, CreatedDay, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(CreatedYear, UpdatedMonth, UpdatedDay, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<string> BlankTexts => new() { string.Empty, Blank };

    [Fact]
    public void Create_Should_ReturnActiveType_When_DataIsValid()
    {
        Result<RoutineType> result = CreateType(PaddedName, Creator);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(OilChangeName);
        result.Value.Code.Value.Should().Be(OilChangeCode);
        result.Value.Status.Should().Be(RoutineTypeStatus.Active);
        result.Value.UpdatedBy.Should().Be(Creator);
        result.Value.UpdatedAt.Should().Be(CreatedAt);
        result.Value.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(BlankTexts))]
    public void Create_Should_ReturnNameRequired_When_NameIsBlank(string name)
    {
        Result<RoutineType> result = CreateType(name, Creator);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NameRequiredCode);
    }

    [Theory]
    [MemberData(nameof(BlankTexts))]
    public void Create_Should_ReturnUpdatedByRequired_When_UpdatedByIsBlank(string updatedBy)
    {
        Result<RoutineType> result = CreateType(OilChangeName, updatedBy);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UpdatedByRequiredCode);
    }

    [Fact]
    public void Create_Should_ReturnNameRequired_When_NameIsNull()
    {
        Result<RoutineType> result = CreateType(null!, Creator);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NameRequiredCode);
    }

    [Fact]
    public void Create_Should_ReturnUpdatedByRequired_When_UpdatedByIsNull()
    {
        Result<RoutineType> result = CreateType(OilChangeName, null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UpdatedByRequiredCode);
    }

    [Fact]
    public void UpdateParameters_Should_ChangeValuesAndRaiseEvent_When_ValuesChange()
    {
        RoutineType routineType = CreateType(OilChangeName, Creator).Value;

        Result result = routineType.UpdateParameters(Interval(NewKm, NewMonths), Threshold(TwentyPercent), PaddedEditor, UpdatedAt);

        result.IsSuccess.Should().BeTrue();
        routineType.DefaultInterval.Kilometers.Should().Be(NewKm);
        routineType.DefaultInterval.Months.Should().Be(NewMonths);
        routineType.DueSoonThreshold.Percent.Should().Be(TwentyPercent);
        routineType.UpdatedBy.Should().Be(Editor);
        routineType.UpdatedAt.Should().Be(UpdatedAt);
        routineType.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new RoutineTypeParametersChanged(routineType.Id, UpdatedAt));
    }

    [Fact]
    public void UpdateParameters_Should_RaiseEvent_When_OnlyThresholdChanges()
    {
        RoutineType routineType = CreateType(OilChangeName, Creator).Value;

        Result result = routineType.UpdateParameters(Interval(OilChangeKm, OilChangeMonths), Threshold(TwentyPercent), Editor, UpdatedAt);

        result.IsSuccess.Should().BeTrue();
        routineType.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void UpdateParameters_Should_NotRaiseEventNorTouchAudit_When_ValuesAreTheSame()
    {
        RoutineType routineType = CreateType(OilChangeName, Creator).Value;

        Result result = routineType.UpdateParameters(Interval(OilChangeKm, OilChangeMonths), Threshold(TenPercent), Editor, UpdatedAt);

        result.IsSuccess.Should().BeTrue();
        routineType.DomainEvents.Should().BeEmpty();
        routineType.UpdatedBy.Should().Be(Creator);
        routineType.UpdatedAt.Should().Be(CreatedAt);
    }

    [Theory]
    [MemberData(nameof(BlankTexts))]
    public void UpdateParameters_Should_ReturnUpdatedByRequiredAndKeepValues_When_UpdatedByIsBlank(string updatedBy)
    {
        RoutineType routineType = CreateType(OilChangeName, Creator).Value;

        Result result = routineType.UpdateParameters(Interval(NewKm, NewMonths), Threshold(TwentyPercent), updatedBy, UpdatedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UpdatedByRequiredCode);
        routineType.DefaultInterval.Kilometers.Should().Be(OilChangeKm);
        routineType.DueSoonThreshold.Percent.Should().Be(TenPercent);
        routineType.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ToParameters_Should_ReturnCurrentValues_When_TypeIsUpdated()
    {
        RoutineType routineType = CreateType(OilChangeName, Creator).Value;
        routineType.UpdateParameters(Interval(NewKm, NewMonths), Threshold(TwentyPercent), Editor, UpdatedAt);

        RoutineTypeParameters parameters = routineType.ToParameters();

        parameters.Code.Should().Be(routineType.Code);
        parameters.Name.Should().Be(OilChangeName);
        parameters.Interval.Kilometers.Should().Be(NewKm);
        parameters.Threshold.Percent.Should().Be(TwentyPercent);
    }

    private static Result<RoutineType> CreateType(string name, string updatedBy) =>
        RoutineType.Create(
            RoutineTypeId.New(),
            RoutineTypeCode.Create(OilChangeCode).Value,
            name,
            Interval(OilChangeKm, OilChangeMonths),
            Threshold(TenPercent),
            updatedBy,
            CreatedAt);

    private static MaintenanceInterval Interval(int kilometers, int months) =>
        MaintenanceInterval.Create(kilometers, months).Value;

    private static DueSoonThreshold Threshold(decimal percent) => DueSoonThreshold.Create(percent).Value;
}
