using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class MaintenanceErrorsTests
{
    private const string PlanNotFound = "MaintenancePlan.NotFound";
    private const string TypeNotFound = "RoutineType.NotFound";
    private const string AlreadyExists = "Routine.AlreadyExists";
    private const string LastServiceAboveOdometer = "Routine.LastServiceKmAboveOdometer";
    private const string IncompleteLastService = "Routine.IncompleteLastService";
    private const string DuplicateInBatch = "Bulk.DuplicateInBatch";
    private const string InvalidTransition = "Alert.InvalidTransition";

    [Fact]
    public void Catalog_Should_ExposeStableCn03Codes_When_Accessed()
    {
        MaintenanceErrors.MaintenancePlanNotFound.Code.Should().Be(PlanNotFound);
        MaintenanceErrors.RoutineTypeNotFound.Code.Should().Be(TypeNotFound);
        MaintenanceErrors.RoutineAlreadyExists.Code.Should().Be(AlreadyExists);
        MaintenanceErrors.RoutineLastServiceKmAboveOdometer.Code.Should().Be(LastServiceAboveOdometer);
        MaintenanceErrors.RoutineIncompleteLastService.Code.Should().Be(IncompleteLastService);
        MaintenanceErrors.BulkDuplicateInBatch.Code.Should().Be(DuplicateInBatch);
        MaintenanceErrors.AlertInvalidTransition.Code.Should().Be(InvalidTransition);
    }

    [Fact]
    public void Catalog_Should_ClassifyErrorTypes_When_Accessed()
    {
        MaintenanceErrors.MaintenancePlanNotFound.Type.Should().Be(ErrorType.NotFound);
        MaintenanceErrors.RoutineAlreadyExists.Type.Should().Be(ErrorType.Conflict);
        MaintenanceErrors.AlertInvalidTransition.Type.Should().Be(ErrorType.BusinessRule);
        MaintenanceErrors.RoutineIncompleteLastService.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Catalog_Should_HaveUniqueCodes_When_AllErrorsAreEnumerated()
    {
        string[] codes = typeof(MaintenanceErrors)
            .GetFields()
            .Where(field => field.FieldType == typeof(Error))
            .Select(field => ((Error)field.GetValue(null)!).Code)
            .ToArray();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().NotBeEmpty();
    }
}
