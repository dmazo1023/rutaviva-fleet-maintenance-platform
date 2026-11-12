using RutaViva.Workshops.Domain;

namespace RutaViva.Workshops.Api.Contracts;

public sealed record ChangeWorkshopStatusRequest(WorkshopStatus? Status);
