using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// Advances one incident's escalation by at most one step. Runs inside a tenant-scoped DI
/// scope, so every query here is filtered normally and the resolvers need no special case.
/// </summary>
public class IncidentEscalator(
    DbContext db,
    ITenantContext tenantContext,
    IEscalationTargetResolver targetResolver,
    INotificationSender notificationSender,
    IOptions<EscalationSettings> settings) : IIncidentEscalator
{
    public async Task<EscalationOutcome> EscalateAsync(
        Guid incidentId, DateTime utcNow, CancellationToken ct)
    {
        // The failure mode this guards is silence, not an error: with no tenant the query
        // filter matches nothing, so every query below would come back empty and the pass
        // would look like a success that happened to find no work.
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException(
                "IncidentEscalator requires a tenant-scoped context. Without one the query "
              + "filter matches nothing and this would silently do no work.");

        var incident = await db.Set<Incident>()
            .FirstOrDefaultAsync(i => i.Id == incidentId, ct);

        if (incident is null
            || incident.Status != IncidentStatus.Triggered
            || incident.EscalationExhaustedAt is not null)
        {
            return EscalationOutcome.Skipped;
        }

        var service = await db.Set<Service>()
            .Where(s => s.Id == incident.ServiceId)
            .Select(s => new { s.Name, s.EscalationPolicyId })
            .FirstOrDefaultAsync(ct);

        if (service?.EscalationPolicyId is not { } policyId)
            return EscalationOutcome.Skipped;

        var policy = await db.Set<EscalationPolicy>()
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.Id == policyId, ct);

        if (policy is null || policy.Tiers.Count == 0)
            return EscalationOutcome.Skipped;

        var context = new EscalationContext(incident, policy, service.Name, tenantId, utcNow);

        var latest = await db.Set<IncidentEscalation>()
            .Where(e => e.IncidentId == incidentId)
            .OrderByDescending(e => e.Level)
            .FirstOrDefaultAsync(ct);

        // A claim that was never sent: finish it before considering the next tier. This is
        // the crash-recovery path that makes delivery at-least-once.
        if (latest is { NotifiedAt: null })
        {
            if (!latest.HasGivenUp(settings.Value.MaxSendAttempts))
                return await NotifyAsync(context, latest, ct);

            // Out of attempts. Escalation still moves on once the next tier falls due,
            // because a broken transport should not freeze the whole chain.
            var outcome = await ClaimNextTierIfDueAsync(context, latest, ct);

            // Report the give-up rather than "not yet due", so a dropped page is visible
            // in the pass summary instead of looking like ordinary waiting.
            return outcome == EscalationOutcome.NotYetDue ? EscalationOutcome.GaveUp : outcome;
        }

        return await ClaimNextTierIfDueAsync(context, latest, ct);
    }

    private async Task<EscalationOutcome> ClaimNextTierIfDueAsync(
        EscalationContext context, IncidentEscalation? latest, CancellationToken ct)
    {
        var nextTier = context.Policy.GetNextTier(latest?.Level ?? 0);

        if (nextTier is null)
        {
            context.Incident.RecordEscalationExhausted(
                $"Escalation policy '{context.Policy.Name}' has no tier beyond "
              + $"{latest?.Level ?? 0}. Nobody acknowledged this incident.");

            await db.SaveChangesAsync(ct);
            return EscalationOutcome.Exhausted;
        }

        var dueAt = DueTimeFor(context, latest);
        if (dueAt > context.UtcNow)
            return EscalationOutcome.NotYetDue;

        var claim = IncidentEscalation.Claim(
            context.TenantId, context.Incident.Id, nextTier.Id, nextTier.Level, dueAt);

        db.Add(claim);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Most likely another instance claimed this tier first, which the unique index
            // over (IncidentId, Level) turns into a failed insert. Reading the row back is
            // provider-independent, unlike inspecting a SQL Server error number, and it
            // avoids swallowing a genuine write failure.
            db.ChangeTracker.Clear();

            var claimedElsewhere = await db.Set<IncidentEscalation>()
                .AnyAsync(e => e.IncidentId == context.Incident.Id
                            && e.Level == nextTier.Level, ct);

            if (!claimedElsewhere)
                throw;

            return EscalationOutcome.Skipped;
        }

        return await NotifyAsync(context, claim, ct);
    }

    private async Task<EscalationOutcome> NotifyAsync(
        EscalationContext context, IncidentEscalation escalation, CancellationToken ct)
    {
        // Counted and committed before the send. Counting after a failure instead would
        // never count a crash mid-send, leaving the retry loop unbounded.
        escalation.RecordAttempt();
        await db.SaveChangesAsync(ct);

        var targets = await targetResolver.ResolveTierTargetsAsync(
            escalation.EscalationTierId, context.Incident.ServiceId, context.UtcNow, ct);

        // Allowed to throw. The escalation stays unstamped and the next pass retries it,
        // which is the whole reason the attempt was counted first.
        await notificationSender.SendAsync(
            new EscalationNotification(
                context.Incident.Id,
                context.Incident.Title,
                context.Incident.Severity,
                context.Incident.ServiceId,
                context.ServiceName,
                escalation.Level,
                targets),
            ct);

        escalation.MarkNotified(
            targets.Select(t => new EscalationRecipient(t.UserId, t.Source)).ToList());

        // The status was checked on load, so an acknowledgement arriving in between is a
        // race this cannot close without locking: a page goes out just as somebody takes
        // the incident. The domain guard reads the loaded status and lets it through.
        context.Incident.RecordEscalation(escalation.Level, DescribeSend(context, escalation, targets.Count));

        await db.SaveChangesAsync(ct);

        return targets.Count == 0 ? EscalationOutcome.ReachedNobody : EscalationOutcome.Notified;
    }

    private static string DescribeSend(
        EscalationContext context, IncidentEscalation escalation, int targetCount) =>
        targetCount == 0
            ? $"Escalated to tier {escalation.Level} of '{context.Policy.Name}', which reached nobody."
            : $"Escalated to tier {escalation.Level} of '{context.Policy.Name}', reaching "
              + $"{targetCount} {(targetCount == 1 ? "person" : "people")}.";

    /// <summary>
    /// The first tier is due as soon as the incident exists. Every later tier is due its
    /// predecessor's timeout after that predecessor went out — or after it became due, when
    /// it never went out at all, so a failed tier still hands over on schedule.
    /// </summary>
    private static DateTime DueTimeFor(EscalationContext context, IncidentEscalation? latest)
    {
        if (latest is null)
            return context.Incident.CreatedAt;

        var previousTier = context.Policy.Tiers.FirstOrDefault(t => t.Level == latest.Level);
        var reference = latest.NotifiedAt ?? latest.DueAt;

        return reference.AddMinutes(previousTier?.EscalateAfterMinutes ?? 0);
    }

    private sealed record EscalationContext(
        Incident Incident,
        EscalationPolicy Policy,
        string ServiceName,
        Guid TenantId,
        DateTime UtcNow);
}
