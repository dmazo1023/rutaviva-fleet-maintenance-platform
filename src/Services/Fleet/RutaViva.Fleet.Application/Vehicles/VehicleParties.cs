using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Application.Vehicles;

internal sealed record VehicleParties(CustomerReference Owner, WorkshopReference Workshop, ApproverReference? Responsible);
