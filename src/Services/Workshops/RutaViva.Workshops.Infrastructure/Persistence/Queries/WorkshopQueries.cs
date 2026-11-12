using Microsoft.EntityFrameworkCore;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Infrastructure.Persistence.Queries;

public sealed class WorkshopQueries(WorkshopsDbContext context) : IWorkshopQueries
{
    private const string CaseInsensitiveCollation = "SQL_Latin1_General_CP1_CI_AS";

    public async Task<IReadOnlyList<WorkshopSummaryDto>> ListAsync(
        WorkshopStatus? status,
        string? city,
        CancellationToken cancellationToken)
    {
        IQueryable<Workshop> filtered = FilterByCity(FilterByStatus(context.Workshops.AsNoTracking(), status), city);
        return await filtered
            .OrderBy(workshop => workshop.Name)
            .ThenBy(workshop => workshop.CreatedAt)
            .Select(workshop => new WorkshopSummaryDto(
                workshop.Id.Value,
                workshop.Name,
                workshop.TaxId.Value,
                workshop.Address.City,
                workshop.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkshopDetailDto?> GetDetailAsync(WorkshopId id, CancellationToken cancellationToken) =>
        await context.Workshops
            .AsNoTracking()
            .Where(workshop => workshop.Id == id)
            .Select(workshop => new WorkshopDetailDto(
                workshop.Id.Value,
                workshop.Name,
                workshop.TaxId.Value,
                workshop.Email.Value,
                workshop.Phone == null ? null : workshop.Phone.Value,
                workshop.Address.Address,
                workshop.Address.City,
                workshop.Status,
                workshop.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<Workshop> FilterByStatus(IQueryable<Workshop> workshops, WorkshopStatus? status)
    {
        if (status is not { } value)
        {
            return workshops;
        }

        return workshops.Where(workshop => workshop.Status == value);
    }

    private static IQueryable<Workshop> FilterByCity(IQueryable<Workshop> workshops, string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return workshops;
        }

        string normalized = city.Trim();
        return workshops.Where(workshop =>
            EF.Functions.Collate(workshop.Address.City, CaseInsensitiveCollation) == normalized);
    }
}
