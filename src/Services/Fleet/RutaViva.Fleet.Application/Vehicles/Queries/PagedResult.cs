namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
