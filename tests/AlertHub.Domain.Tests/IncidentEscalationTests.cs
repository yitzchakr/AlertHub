using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Tests;

public class IncidentEscalationTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid IncidentId = Guid.NewGuid();
    private static readonly Guid TierId = Guid.NewGuid();
    private static readonly DateTime Due = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static IncidentEscalation AClaim() =>
        IncidentEscalation.Claim(Tenant, IncidentId, TierId, 1, Due);

    [Fact]
    public void Claim_records_the_tier_it_is_responsible_for()
    {
        var escalation = AClaim();

        Assert.NotEqual(Guid.Empty, escalation.Id);
        Assert.Equal(Tenant, escalation.TenantId);
        Assert.Equal(IncidentId, escalation.IncidentId);
        Assert.Equal(TierId, escalation.EscalationTierId);
        Assert.Equal(1, escalation.Level);
        Assert.Equal(Due, escalation.DueAt);
    }

    // Unsent is the initial state, and it is what makes delivery at-least-once: a crash
    // between claiming and sending leaves a row the next pass picks up.
    [Fact]
    public void A_fresh_claim_is_not_yet_notified()
    {
        Assert.Null(AClaim().NotifiedAt);
        Assert.Empty(AClaim().Targets);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Claim_rejects_a_level_that_is_not_positive(int level)
    {
        Assert.Throws<DomainException>(() =>
            IncidentEscalation.Claim(Tenant, IncidentId, TierId, level, Due));
    }

    [Fact]
    public void Claim_reads_an_offsetless_due_time_as_utc()
    {
        var naive = DateTime.SpecifyKind(Due, DateTimeKind.Unspecified);

        var escalation = IncidentEscalation.Claim(Tenant, IncidentId, TierId, 1, naive);

        Assert.Equal(DateTimeKind.Utc, escalation.DueAt.Kind);
        Assert.Equal(Due, escalation.DueAt);
    }

    [Fact]
    public void MarkNotified_records_the_recipients_and_stamps_the_send()
    {
        var escalation = AClaim();
        var dana = Guid.NewGuid();
        var eli = Guid.NewGuid();

        escalation.MarkNotified(
        [
            new EscalationRecipient(dana, EscalationTargetSource.OnCall),
            new EscalationRecipient(eli, EscalationTargetSource.Role),
        ]);

        Assert.NotNull(escalation.NotifiedAt);
        Assert.Equal(2, escalation.Targets.Count);
        Assert.Contains(escalation.Targets, t =>
            t.UserId == dana && t.Source == EscalationTargetSource.OnCall);
        Assert.Contains(escalation.Targets, t =>
            t.UserId == eli && t.Source == EscalationTargetSource.Role);
    }

    [Fact]
    public void MarkNotified_carries_the_tenant_onto_every_recipient()
    {
        var escalation = AClaim();

        escalation.MarkNotified([new EscalationRecipient(Guid.NewGuid(), EscalationTargetSource.Role)]);

        Assert.All(escalation.Targets, t => Assert.Equal(Tenant, t.TenantId));
    }

    // A repeated user would violate the unique index and fail the whole save, so the
    // escalation would be blocked rather than just that one recipient dropped.
    [Fact]
    public void MarkNotified_keeps_one_row_per_person()
    {
        var escalation = AClaim();
        var dana = Guid.NewGuid();

        escalation.MarkNotified(
        [
            new EscalationRecipient(dana, EscalationTargetSource.DirectUser),
            new EscalationRecipient(dana, EscalationTargetSource.OnCall),
        ]);

        var target = Assert.Single(escalation.Targets);
        // First source wins, matching the resolver's own precedence.
        Assert.Equal(EscalationTargetSource.DirectUser, target.Source);
    }

    // Reaching nobody is a real outcome, not an error: the tier was configured but the
    // roster had a gap. It must still be stamped, or it would be retried forever.
    [Fact]
    public void MarkNotified_accepts_reaching_nobody()
    {
        var escalation = AClaim();

        escalation.MarkNotified([]);

        Assert.NotNull(escalation.NotifiedAt);
        Assert.Empty(escalation.Targets);
    }

    [Fact]
    public void MarkNotified_refuses_a_second_time()
    {
        var escalation = AClaim();
        escalation.MarkNotified([new EscalationRecipient(Guid.NewGuid(), EscalationTargetSource.Role)]);

        var ex = Assert.Throws<DomainException>(() => escalation.MarkNotified([]));

        Assert.Contains("already recorded as notified", ex.Message);
        Assert.Single(escalation.Targets);
    }
}

public class IncidentEscalationRecordingTests
{
    private static Incident ATriggeredIncident() =>
        Incident.Create(Guid.NewGuid(), Guid.NewGuid(), "Checkout down", IncidentSeverity.P1_Critical);

    [Fact]
    public void RecordEscalation_adds_an_escalated_event()
    {
        var incident = ATriggeredIncident();

        incident.RecordEscalation(2, "Escalated to tier 2, reaching 3 people.");

        var escalated = Assert.Single(incident.Events, e => e.EventType == IncidentEventType.Escalated);
        Assert.Contains("tier 2", escalated.Description);
    }

    // The authoritative stop condition. A scan can decide an incident is due and an
    // acknowledgement can land before the write; this is what refuses it.
    [Fact]
    public void RecordEscalation_is_refused_once_somebody_has_acknowledged()
    {
        var incident = ATriggeredIncident();
        incident.Acknowledge(Guid.NewGuid());

        var ex = Assert.Throws<DomainException>(() => incident.RecordEscalation(2, "too late"));

        Assert.Contains("Acknowledged", ex.Message);
        Assert.DoesNotContain(incident.Events, e => e.EventType == IncidentEventType.Escalated);
    }

    [Fact]
    public void RecordEscalation_is_refused_once_resolved()
    {
        var incident = ATriggeredIncident();
        incident.Acknowledge(Guid.NewGuid());
        incident.Resolve("Rolled back the deploy.");

        Assert.Throws<DomainException>(() => incident.RecordEscalation(2, "too late"));
    }

    [Fact]
    public void RecordEscalation_can_happen_more_than_once_while_still_triggered()
    {
        var incident = ATriggeredIncident();

        incident.RecordEscalation(1, "tier 1");
        incident.RecordEscalation(2, "tier 2");

        Assert.Equal(2, incident.Events.Count(e => e.EventType == IncidentEventType.Escalated));
    }
}
