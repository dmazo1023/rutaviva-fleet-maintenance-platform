using RutaViva.Fleet.Application.Vehicles.Queries;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Infrastructure.Queries;

internal static class VehicleProjector
{
    public static VehicleSummaryDto ToSummary(VehicleReadRow row) =>
        new(
            row.Vehicle.Id,
            row.Vehicle.Plate.Value,
            row.Vehicle.Vin?.Value,
            row.Vehicle.Description.Brand,
            row.Vehicle.Description.Model,
            row.Vehicle.Description.ModelYear,
            row.Vehicle.Status,
            row.Vehicle.StatusChangedAt,
            row.Owner.Id,
            row.Owner.Name,
            row.Workshop.Id,
            row.Workshop.Name,
            row.Responsible?.Id,
            row.Responsible?.FullName);

    public static VehicleDetailDto ToDetail(VehicleReadRow row) =>
        new(
            row.Vehicle.Id,
            row.Vehicle.Plate.Value,
            row.Vehicle.Vin?.Value,
            row.Vehicle.Description.Brand,
            row.Vehicle.Description.Model,
            row.Vehicle.Description.ModelYear,
            row.Vehicle.InitialOdometer.Value,
            row.Vehicle.Status,
            row.Vehicle.RegisteredAt,
            row.Vehicle.StatusChangedAt,
            new VehicleOwnerDto(row.Owner.Id, row.Owner.Name, row.Owner.TaxId),
            new VehicleWorkshopDto(row.Workshop.Id, row.Workshop.Name),
            ToResponsible(row.Responsible));

    private static VehicleResponsibleDto? ToResponsible(ApproverReference? responsible)
    {
        if (responsible is null)
        {
            return null;
        }

        return new VehicleResponsibleDto(responsible.Id, responsible.FullName, responsible.Email);
    }
}
