using AlertHub.Application.Features.Incidents;
using AlertHub.Application.Features.Incidents.GetIncidentDetail;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

public class GetIncidentDetailTests
{
    private static readonly DateTime Due = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Task<IncidentDetailDto?> Detail(TestDb fixture, Guid? incidentId = null) =>
        GetIncidentDetailHandler.HandleAsync(
            new GetIncidentDetailQuery(incidentId ?? fixture.IncidentId),
            fixture.Db,
            CancellationToken.None);

    private static IncidentEscalation AddEscalation(
        TestDb fixture, Guid tierId, int level, params EscalationRecipient[] recipients)
    {
        var escalation = IncidentEscalation.Claim(
            fixture.TenantId, fixture.IncidentId, tierId, level, Due);

        escalation.RecordAttempt();
        escalation.MarkNotified(recipients);

        fixture.Db.Add(escalation);
        fixture.Db.SaveChanges();
        fixture.Db.ChangeTracker.Clear();

        return escalation;
    }

    [Fact]
    public async Task It_returns_the_incident_with_its_service_and_policy()
    {
        using var fixture = new TestDb();

        var detail = await Detail(fixture);

        Assert.NotNull(detail);
        Assert.Equal(fixture.IncidentId, detail.Id);
        Assert.Equal("Payments API", detail.ServiceName);
        Assert.Equal("Payments Critical", detail.EscalationPolicyName);
        Assert.Equal(IncidentStatus.Triggered, detail.Status);
        Assert.Equal(IncidentSeverity.P1_Critical, detail.Severity);
    }

    [Fact]
    public async Task An_unknown_incident_returns_nothing()
    {
        using var fixture = new TestDb();

        Assert.Null(await Detail(fixture, Guid.NewGuid()));
    }

    [Fact]
    public async Task Another_tenant_cannot_read_the_incident()
    {
        using var fixture = new TestDb();

        var own = await Detail(fixture);

        fixture.AsTenant(fixture.OtherTenantId);
        var foreign = await Detail(fixture);

        // The null only means something beside a hit proving the row exists.
        Assert.NotNull(own);
        Assert.Null(foreign);
    }

    [Fact]
    public async Task The_timeline_comes_back_oldest_first()
    {
        using var fixture = new TestDb();
        var incident = fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId);
        incident.RecordEscalation(1, "Escalated to tier 1.");
        incident.Acknowledge(fixture.ResponderId, "Dana Responder");
        fixture.Db.SaveChanges();
        fixture.Db.ChangeTracker.Clear();

        var detail = await Detail(fixture);

        Assert.Equal(3, detail!.Events.Count);
        Assert.Equal(IncidentEventType.Created, detail.Events[0].EventType);
        Assert.Equal(
            detail.Events.OrderBy(e => e.OccurredAt).Select(e => e.OccurredAt),
            detail.Events.Select(e => e.OccurredAt));
    }

    // The reason this endpoint exists: escalation history has to be visible somewhere other
    // than the server log.
    [Fact]
    public async Task Escalations_come_back_with_who_each_tier_reached()
    {
        using var fixture = new TestDb();
        AddEscalation(fixture, fixture.MixedTierId, 1,
            new EscalationRecipient(fixture.ResponderId, EscalationTargetSource.Role),
            new EscalationRecipient(fixture.AdminId, EscalationTargetSource.OnCall));

        var detail = await Detail(fixture);

        var escalation = Assert.Single(detail!.Escalations);
        Assert.Equal(1, escalation.Level);
        Assert.Equal(5, escalation.EscalateAfterMinutes);
        Assert.Equal(1, escalation.Attempts);
        Assert.NotNull(escalation.NotifiedAt);
        Assert.Equal(2, escalation.Targets.Count);
        Assert.Contains(escalation.Targets, t =>
            t.DisplayName == "Avery Admin" && t.Source == EscalationTargetSource.OnCall);
        Assert.Contains(escalation.Targets, t => t.Email == "dana@acme.test");
    }

    [Fact]
    public async Task Escalations_come_back_in_tier_order()
    {
        using var fixture = new TestDb();
        AddEscalation(fixture, fixture.OnCallOnlyTierId, 2);
        AddEscalation(fixture, fixture.MixedTierId, 1);

        var detail = await Detail(fixture);

        Assert.Equal([1, 2], detail!.Escalations.Select(e => e.Level));
    }

    // A tier that fired but reached nobody is the coverage gap made visible, so it must come
    // back as a real row with no targets rather than being absent.
    [Fact]
    public async Task A_tier_that_reached_nobody_still_appears()
    {
        using var fixture = new TestDb();
        AddEscalation(fixture, fixture.OnCallOnlyTierId, 2);

        var detail = await Detail(fixture);

        var escalation = Assert.Single(detail!.Escalations);
        Assert.NotNull(escalation.NotifiedAt);
        Assert.Empty(escalation.Targets);
    }

    [Fact]
    public async Task An_incident_that_ran_out_of_tiers_reports_it()
    {
        using var fixture = new TestDb();
        var incident = fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId);
        incident.RecordEscalationExhausted("Policy exhausted; nobody acknowledged.");
        fixture.Db.SaveChanges();
        fixture.Db.ChangeTracker.Clear();

        var detail = await Detail(fixture);

        Assert.NotNull(detail!.EscalationExhaustedAt);
    }

    [Fact]
    public async Task An_acknowledged_incident_names_who_took_it()
    {
        using var fixture = new TestDb();
        var incident = fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId);
        incident.Acknowledge(fixture.ResponderId, "Dana Responder");
        fixture.Db.SaveChanges();
        fixture.Db.ChangeTracker.Clear();

        var detail = await Detail(fixture);

        Assert.Equal("Dana Responder", detail!.AcknowledgedByDisplayName);
        Assert.NotNull(detail.AcknowledgedAt);
    }

    [Fact]
    public async Task A_fresh_incident_has_no_escalations_and_one_event()
    {
        using var fixture = new TestDb();

        var detail = await Detail(fixture);

        Assert.Empty(detail!.Escalations);
        Assert.Single(detail.Events);
        Assert.Null(detail.AcknowledgedByDisplayName);
        Assert.Null(detail.EscalationExhaustedAt);
    }
}

/// <summary>
/// The timeline is the record a person reads afterwards. An identifier in it tells them
/// nothing, so these assert that names reach the text itself, not merely the structured
/// fields beside it.
/// </summary>
public class IncidentTimelineNamingTests
{
    [Fact]
    public void Acknowledging_names_the_person_rather_than_their_id()
    {
        var incident = Incident.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Checkout down", IncidentSeverity.P1_Critical);
        var userId = Guid.NewGuid();

        incident.Acknowledge(userId, "Dana Responder");

        var acknowledged = Assert.Single(
            incident.Events, e => e.EventType == IncidentEventType.Acknowledged);

        Assert.Equal("Acknowledged by Dana Responder", acknowledged.Description);
        Assert.DoesNotContain(userId.ToString(), acknowledged.Description);
    }

    [Fact]
    public async Task An_escalation_event_names_who_it_reached()
    {
        using var fixture = new TestDb();

        await IncidentEscalatorTests.EscalateOnce(fixture);

        fixture.Db.ChangeTracker.Clear();
        var escalated = fixture.Db.Set<IncidentEvent>()
            .Single(e => e.EventType == IncidentEventType.Escalated);

        Assert.Contains("Dana Responder", escalated.Description);
        Assert.DoesNotContain("people.", escalated.Description);
    }
}
