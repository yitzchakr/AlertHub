namespace AlertHub.Domain.Entities;

/// <summary>
/// One person an escalation reached. Kept as rows rather than a count so an incident's
/// history can answer who was actually paged, which is what makes per-responder reporting
/// possible later. Delivery status is deliberately absent: nothing transports a
/// notification yet, and columns nothing writes are worse than an additive migration.
/// </summary>
public class IncidentEscalationTarget : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid IncidentEscalationId { get; private set; }
    public Guid UserId { get; private set; }
    public EscalationTargetSource Source { get; private set; }

    // Navigation
    public IncidentEscalation Escalation { get; private set; } = default!;

    /// <summary>
    /// A restricted foreign key rather than a snapshot of the name: the constraint keeps
    /// the user resolvable, so the history never points at somebody who cannot be named.
    /// </summary>
    public User User { get; private set; } = default!;

    private IncidentEscalationTarget() { }

    public static IncidentEscalationTarget Create(
        Guid escalationId, Guid tenantId, Guid userId, EscalationTargetSource source)
    {
        return new IncidentEscalationTarget
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IncidentEscalationId = escalationId,
            UserId = userId,
            Source = source
        };
    }
}
