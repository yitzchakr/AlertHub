using AlertHub.Application.Common.Models;
using AlertHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Incidents.GetIncidentsForService;

public static class GetIncidentsForServiceHandler
{
    public static async Task<PagedResult<IncidentSummaryDto>> HandleAsync(
        GetIncidentsForServiceQuery query,
        AlertHubDbContext db,
        CancellationToken ct)
    {
        var baseQuery = db.Incidents
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
