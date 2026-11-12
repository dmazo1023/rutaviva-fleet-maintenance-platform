using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Infrastructure.Queries;

internal sealed record VehicleReadRow(
    Vehicle Vehicle,
    CustomerReference Owner,
    WorkshopReference Workshop,
    ApproverReference? Responsible);
