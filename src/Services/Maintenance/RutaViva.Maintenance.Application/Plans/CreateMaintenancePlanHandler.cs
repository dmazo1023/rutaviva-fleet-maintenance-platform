using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Application.Plans;

public sealed class CreateMaintenancePlanHandler(IMaintenancePlanRepository plans, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateMaintenancePlanCommand, VehicleId>
{
    public async Task<Result<VehicleId>> HandleAsync(CreateMaintenancePlanCommand command, CancellationToken cancellationToken)
    {
        MaintenancePlan? existing = await plans.GetAsync(command.VehicleId, cancellationToken);
        if (existing is not null)
        {
            return command.VehicleId;
        }

        Result<MaintenancePlan> plan = BuildPlan(command);
        if (plan.IsFailure)
        {
            return plan.Error;
        }

        plans.Add(plan.Value);
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.VehicleId;
    }

    private static Result<MaintenancePlan> BuildPlan(CreateMaintenancePlanCommand command)
    {
        Result<LicensePlate> plate = LicensePlate.Create(command.Plate);
        if (plate.IsFailure)
        {
            return plate.Error;
        }

        Result<Odometer> odometer = Odometer.Create(command.OdometerKm, command.RegisteredAt);
        if (odometer.IsFailure)
        {
            return odometer.Error;
        }

        return MaintenancePlan.Create(
            command.VehicleId,
            plate.Value,
            command.CustomerId,
            command.Status,
            odometer.Value,
            MaintenanceTimeZone.ToLocalDate(command.RegisteredAt));
    }
}
