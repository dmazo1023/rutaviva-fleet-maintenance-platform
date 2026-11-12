using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.ServiceOrders.Domain.References;

public sealed class RoutineReference : Entity<Guid>
{
    private RoutineReference()
    {
        RoutineName = null!;
        RoutineTypeCode = null!;
    }

    public Guid VehicleId { get; private set; }

    public string RoutineName { get; private set; }

    public string RoutineTypeCode { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static RoutineReference Create(Guid id, Guid vehicleId, string routineName, string routineTypeCode, DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            VehicleId = vehicleId,
            RoutineName = routineName,
            RoutineTypeCode = routineTypeCode,
            UpdatedAt = updatedAt
        };

    public void Apply(RoutineReference source)
    {
        VehicleId = source.VehicleId;
        RoutineName = source.RoutineName;
        RoutineTypeCode = source.RoutineTypeCode;
        UpdatedAt = source.UpdatedAt;
    }
}
