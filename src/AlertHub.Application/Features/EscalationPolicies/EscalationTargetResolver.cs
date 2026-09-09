using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AlertHub.Application.Features.EscalationPolicies;

public class EscalationTargetResolver(DbContext db, IOnCallResolver onCall) : IEscalationTargetResolver
{
    public async Task<IReadOnlyList<EscalationTargetDto>> ResolveTierTargetsAsync(
        Guid tierId, Guid serviceId, DateTime atUtc, CancellationToken ct)
    {
        var assignments = await db.Set<EscalationTierAssignment>()
            .Where(a => a.EscalationTierId == tierId)
            .Select(a => new { a.UserId, a.EscalationRole, a.UseOnCallSchedule })
            .ToListAsync(ct);

        // One person can be reached by more than one assignment. First source wins, in
        // the order below — most specific first, so being named directly beats being
        // caught by a role sweep.
        var targets = new Dictionary<Guid, EscalationTargetDto>();

        var directIds = assignments
            .Where(a => a.UserId.HasValue)
            .Select(a => a.UserId!.Value)
            .ToList();

        if (directIds.Count > 0)
        {
            foreach (var target in await LoadUsersAsync(
                u => directIds.Contains(u.Id), EscalationTargetSource.DirectUser, ct))
            {
                targets.TryAdd(target.UserId, target);
            }
        }

        var roles = assignments
            .Where(a => a.EscalationRole.HasValue)
            .Select(a => a.EscalationRole!.Value)
            .Distinct()
            .ToList();

        if (roles.Count > 0)
        {
            foreach (var target in await LoadUsersAsync(
                u => roles.Contains(u.EscalationRole), EscalationTargetSource.Role, ct))
            {
                targets.TryAdd(target.UserId, target);
            }
        }

        if (assignments.Any(a => a.UseOnCallSchedule)
            && await onCall.ResolveAsync(serviceId, atUtc, ct) is { } person)
        {
            targets.TryAdd(person.UserId, new EscalationTargetDto(
                person.UserId, person.DisplayName, person.Email, EscalationTargetSource.OnCall));
        }

        // An empty list is meaningful: the tier is configured but reaches nobody right
        // now. That is the state this module exists to make visible before an outage.
        return targets.Values.ToList();
    }

    private Task<List<EscalationTargetDto>> LoadUsersAsync(
        Expression<Func<User, bool>> predicate,
        EscalationTargetSource source,
        CancellationToken ct) =>
        db.Set<User>()
            .Where(predicate)
            .Select(u => new EscalationTargetDto(u.Id, u.DisplayName, u.Email, source))
            .ToListAsync(ct);
}
