using System.Text.Json;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

internal static class ScenarioJson
{
    private const string RoutinesProperty = "routines";
    private const string RoutineTypeCodeProperty = "routineTypeCode";
    private const string RoutineIdProperty = "routineId";
    private const string DueStatusProperty = "dueStatus";
    private const string ConsumedPercentProperty = "consumedPercent";
    private const string CycleNumberProperty = "cycleNumber";
    private const string CycleStartKmProperty = "cycleStartKm";
    private const string PlanStatusProperty = "planStatus";
    private const string DueSoonCountProperty = "dueSoonCount";
    private const string OverdueCountProperty = "overdueCount";
    private const string OutcomeProperty = "outcome";
    private const string ReasonProperty = "reason";
    private const string PlateProperty = "plate";
    private const string TypeProperty = "type";
    private const string StatusProperty = "status";
    private const string TriggeredByProperty = "triggeredBy";
    private const string ResolutionReasonProperty = "resolutionReason";
    private const string EvaluatedPlansProperty = "evaluatedPlans";
    private const string DueSoonProperty = "dueSoon";
    private const string OverdueProperty = "overdue";
    private const string OrdersRequestedProperty = "ordersRequested";

    public static JsonElement RoutineOf(JsonElement plan, string routineTypeCode) =>
        plan.GetProperty(RoutinesProperty)
            .EnumerateArray()
            .Single(routine => routine.GetProperty(RoutineTypeCodeProperty).GetString() == routineTypeCode);

    public static string DueStatusOf(JsonElement plan, string routineTypeCode) =>
        RoutineOf(plan, routineTypeCode).GetProperty(DueStatusProperty).GetString()!;

    public static decimal ConsumedPercentOf(JsonElement plan, string routineTypeCode) =>
        RoutineOf(plan, routineTypeCode).GetProperty(ConsumedPercentProperty).GetDecimal();

    public static int CycleNumberOf(JsonElement plan, string routineTypeCode) =>
        RoutineOf(plan, routineTypeCode).GetProperty(CycleNumberProperty).GetInt32();

    public static long CycleStartKmOf(JsonElement plan, string routineTypeCode) =>
        RoutineOf(plan, routineTypeCode).GetProperty(CycleStartKmProperty).GetInt64();

    public static Guid RoutineIdOf(JsonElement plan, string routineTypeCode) =>
        Guid.Parse(RoutineOf(plan, routineTypeCode).GetProperty(RoutineIdProperty).GetString()!);

    public static string PlanStatusOf(JsonElement plan) => plan.GetProperty(PlanStatusProperty).GetString()!;

    public static int DueSoonCountOf(JsonElement summary) => summary.GetProperty(DueSoonCountProperty).GetInt32();

    public static int OverdueCountOf(JsonElement summary) => summary.GetProperty(OverdueCountProperty).GetInt32();

    public static string BulkOutcomeOf(JsonElement row) => row.GetProperty(OutcomeProperty).GetString()!;

    public static string OutcomeOf(JsonElement response) => response.GetProperty(OutcomeProperty).GetString()!;

    public static string? ReasonOf(JsonElement response) => OptionalString(response, ReasonProperty);

    public static string PlateOf(JsonElement alert) => alert.GetProperty(PlateProperty).GetString()!;

    public static string AlertTypeOf(JsonElement alert) => alert.GetProperty(TypeProperty).GetString()!;

    public static string AlertStatusOf(JsonElement alert) => alert.GetProperty(StatusProperty).GetString()!;

    public static string TriggerOf(JsonElement alert) => alert.GetProperty(TriggeredByProperty).GetString()!;

    public static string? ResolutionReasonOf(JsonElement alert) => OptionalString(alert, ResolutionReasonProperty);

    public static int EvaluatedPlansOf(JsonElement summary) => summary.GetProperty(EvaluatedPlansProperty).GetInt32();

    public static int NewDueSoonOf(JsonElement summary) => summary.GetProperty(DueSoonProperty).GetInt32();

    public static int NewOverdueOf(JsonElement summary) => summary.GetProperty(OverdueProperty).GetInt32();

    public static int OrdersRequestedOf(JsonElement summary) => summary.GetProperty(OrdersRequestedProperty).GetInt32();

    private static string? OptionalString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
