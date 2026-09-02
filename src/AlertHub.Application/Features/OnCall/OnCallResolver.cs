using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.OnCall;

public class OnCallResolver(DbContext db) : IOnCallResolver
{
    public Task<OnCallUserDto?> ResolveAsync(Guid serviceId, DateTime atUtc, CancellationToken ct) =>
        db.Set<OnCallShift>()
            .ActiveAt(serviceId, TimeWindow.NormalizeToUtc(atUtc))
            // Overlaps are rejected on write, so at most one row is expected. Ordering
            // anyway means a pair that predates the rule — or slipped through the
            // check-and-insert race — still yields a deterministic answer.
            .OrderByDescending(s => s.Window.Start)
            .Select(s => new OnCallUserDto(
                s.UserId,
                s.User.DisplayName,
                s.User.Email,
                s.Id,
                s.Window.Start,
                s.Window.End))
            .FirstOrDefaultAsync(ct);
}
