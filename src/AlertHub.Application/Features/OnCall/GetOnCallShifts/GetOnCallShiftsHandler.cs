using AlertHub.Application.Common.Models;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.OnCall.GetOnCallShifts;

public static class GetOnCallShiftsHandler
{
    public static async Task<PagedResult<OnCallShiftDto>> HandleAsync(
        GetOnCallShiftsQuery query,
        DbContext db,
        CancellationToken ct)
    {
        var baseQuery = db.Set<OnCallShift>().IntersectingRange(query.FromUtc, query.ToUtc);

        if (query.ServiceId is { } serviceId)
            baseQuery = baseQuery.Where(s => s.ServiceId == serviceId);

        if (query.UserId is { } userId)
            baseQuery = baseQuery.Where(s => s.UserId == userId);

        // Ascending: a roster reads forward in time, unlike the newest-first incident list.
        var ordered = baseQuery.OrderBy(s => s.Window.Start);

        var totalCount = await ordered.CountAsync(ct);

        var items = await ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new OnCallShiftDto(
                s.Id,
                s.ServiceId,
                s.Service.Name,
                s.UserId,
                s.User.DisplayName,
                s.User.Email,
                s.Window.Start,
                s.Window.End,
                s.TimeZoneId))
            .ToListAsync(ct);

        return new PagedResult<OnCallShiftDto>(
            items, totalCount, query.PageNumber, query.PageSize);
    }
}
