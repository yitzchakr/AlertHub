using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// One pass over every tenant's outstanding incidents. Finding the work is a single
/// cross-tenant read; doing the work happens per incident in its own tenant-scoped scope,
/// so the filter bypass stays confined to one query rather than spreading through the
/// module.
/// </summary>
public class EscalationScanner(
    IServiceScopeFactory scopeFactory,
    ILogger<EscalationScanner> logger)
{
    public async Task<EscalationPassSummary> RunAsync(DateTime utcNow, CancellationToken ct)
    {
        var candidates = await FindCandidatesAsync(ct);
        var summary = new EscalationPassSummary { Considered = candidates.Count };

        foreach (var candidate in candidates)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                summary.Record(await EscalateAsync(candidate, utcNow, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One incident must not take the pass down with it, or a single bad row
                // would stop every other tenant's escalations indefinitely.
                summary.Failed++;
                logger.LogError(ex,
                    "Escalating incident {IncidentId} for tenant {TenantId} failed; the "
                  + "next pass will retry it.",
                    candidate.IncidentId, candidate.TenantId);
            }
        }

        return summary;
    }

    /// <summary>
    /// The module's only cross-tenant read. A background scope has no token, so the global
    /// filter would collapse to Guid.Empty and match nothing; ignoring it here is what
    /// makes the worker able to see anything at all. The projection deliberately returns
    /// nothing but identifiers.
    /// </summary>
    private async Task<List<EscalationCandidate>> FindCandidatesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        return await db.Set<Incident>()
            .IgnoreQueryFilters()
            .Where(i => i.Status == IncidentStatus.Triggered
                     && i.EscalationExhaustedAt == null
                     && i.Service.EscalationPolicyId != null)
            .Select(i => new EscalationCandidate(i.Id, i.TenantId))
            .ToListAsync(ct);
    }

    private async Task<EscalationOutcome> EscalateAsync(
        EscalationCandidate candidate, DateTime utcNow, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        // Assigned before anything else resolves, so every query in this scope is filtered
        // to the tenant that owns the incident.
        scope.ServiceProvider.GetRequiredService<ITenantScope>().SetTenant(candidate.TenantId);

        var escalator = scope.ServiceProvider.GetRequiredService<IIncidentEscalator>();
        return await escalator.EscalateAsync(candidate.IncidentId, utcNow, ct);
    }

    private readonly record struct EscalationCandidate(Guid IncidentId, Guid TenantId);
}
