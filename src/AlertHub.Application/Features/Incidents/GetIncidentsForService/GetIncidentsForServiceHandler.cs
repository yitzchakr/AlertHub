using AlertHub.Application.Common.Models;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.GetIncidentsForService;

public static class GetIncidentsForServiceHandler
{
    public static async Task<PagedResult<IncidentSummaryDto>> HandleAsync(
        GetIncidentsForServiceQuery query,
        DbContext db,
        CancellationToken ct)
    {
        var baseQuery = db.Set<Incident>()
            .Where(i => i.ServiceId == query.ServiceId)
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new IncidentSummaryDto
            (
                i.Id,
                i.Title,
                i.Severity,
                i.Status,
                i.CreatedAt
            ))
            .ToListAsync(ct);
        return new PagedResult<IncidentSummaryDto>(
            items,
            totalCount,
            query.PageNumber,
            query.PageSize);

    }
}
