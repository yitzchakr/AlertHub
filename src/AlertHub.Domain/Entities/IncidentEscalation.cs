using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Entities;

/// <summary>
/// A record that one escalation tier was reached for one incident. A ledger rather than
/// columns on Incident, because it answers who was told and when — and because a unique
/// index over (IncidentId, Level) is what makes escalation safe to run from more than one
/// process: the second instance to claim a tier fails its insert and moves on.
/// </summary>
public class IncidentEscalation : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid IncidentId { get; private set; }
    public Guid EscalationTierId { get; private set; }
    public int Level { get; private set; }

    /// <summary>
    /// When this tier became eligible: the previous tier's notification plus its
    /// EscalateAfterMinutes, or the incident's creation for the first tier.
    /// </summary>
    public DateTime DueAt { get; private set; }

    /// <summary>
    /// Null until the notification actually went out. A row left null is a crash between
    /// claiming the tier and sending, and is retried on the next pass — delivery is
    /// at-least-once, because for paging a duplicate beats a silent miss.
    /// </summary>
    public DateTime? NotifiedAt { get; private set; }

    // Navigation
    public Incident Incident { get; private set; } = default!;
    public EscalationTier Tier { get; private set; } = default!;
    public ICollection<IncidentEscalationTarget> Targets { get; private set; } = [];

    private IncidentEscalation() { }

    /// <summary>
    /// Named for what inserting the row means: this process has claimed responsibility for
    /// notifying that tier, and the unique index makes the claim exclusive.
    /// </summary>
    public static IncidentEscalation Claim(
        Guid tenantId, Guid incidentId, Guid tierId, int level, DateTime dueAt)
    {
        if (level <= 0)
            throw new DomainException("Escalation level must be greater than zero.");

        return new IncidentEscalation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IncidentId = incidentId,
            EscalationTierId = tierId,
            Level = level,
            DueAt = TimeWindow.NormalizeToUtc(dueAt)
        };
    }

    /// <summary>
    /// Records the recipients and stamps the send in one step, so an escalation cannot be
    /// marked sent without saying who it reached. An empty collection is legitimate: the
    /// tier was configured but resolved to nobody, which is a coverage gap worth seeing.
    /// </summary>
    public void MarkNotified(IReadOnlyCollection<EscalationRecipient> recipients)
    {
        if (NotifiedAt is not null)
            throw new DomainException(
                "This escalation is already recorded as notified. Claim the next tier instead.");

        // Deduplicated defensively: a repeated user would violate the unique index and
        // fail the whole save, blocking the escalation rather than just that recipient.
        // First source wins, matching the resolver's own precedence.
        foreach (var recipient in recipients.DistinctBy(r => r.UserId))
        {
            Targets.Add(IncidentEscalationTarget.Create(
                Id, TenantId, recipient.UserId, recipient.Source));
        }

        NotifiedAt = DateTime.UtcNow;
    }
}
