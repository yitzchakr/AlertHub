using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.Escalations;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Tests;

public class IncidentEscalatorTests
{
    // Tier timeouts in the fixture: level 1 after 5 minutes, level 2 after 10, level 3 after 20.
    private const int Tier1TimeoutMinutes = 5;

    private static IncidentEscalator EscalatorFor(
        TestDb fixture, INotificationSender sender, int maxSendAttempts = 3) =>
        new(fixture.Db,
            fixture.TenantContext,
            new EscalationTargetResolver(fixture.Db, new OnCallResolver(fixture.Db)),
            sender,
            Options.Create(new EscalationSettings { MaxSendAttempts = maxSendAttempts }));

    /// <summary>Shared with the timeline-naming tests, which assert on what it writes.</summary>
    internal static Task<EscalationOutcome> EscalateOnce(TestDb fixture) =>
        EscalatorFor(fixture, new RecordingSender())
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

    private static DateTime CreatedAtOf(TestDb fixture) =>
        fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId).CreatedAt;

    // --- the first tier ---

    [Fact]
    public async Task The_first_tier_fires_as_soon_as_the_incident_exists()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();

        var outcome = await EscalatorFor(fixture, sender)
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

        Assert.Equal(EscalationOutcome.Notified, outcome);
        var notification = Assert.Single(sender.Sent);
        Assert.Equal(1, notification.Level);
        Assert.Equal("Payments API", notification.ServiceName);
    }

    [Fact]
    public async Task Firing_a_tier_records_the_claim_and_its_recipients()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.AdminId,
            CreatedAtOf(fixture).AddHours(-1), CreatedAtOf(fixture).AddHours(1));

        await EscalatorFor(fixture, new RecordingSender())
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

        fixture.Db.ChangeTracker.Clear();
        var escalation = fixture.Db.Set<IncidentEscalation>().Include(e => e.Targets).Single();

        Assert.Equal(1, escalation.Level);
        Assert.NotNull(escalation.NotifiedAt);
        Assert.Equal(1, escalation.Attempts);
        // Two responders from the role sweep, plus the admin who is on call.
        Assert.Equal(3, escalation.Targets.Count);
        Assert.Contains(escalation.Targets, t => t.Source == EscalationTargetSource.OnCall);
    }

    [Fact]
    public async Task Firing_a_tier_writes_the_incident_timeline()
    {
        using var fixture = new TestDb();

        await EscalatorFor(fixture, new RecordingSender())
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

        fixture.Db.ChangeTracker.Clear();
        var events = fixture.Db.Set<IncidentEvent>()
            .Where(e => e.EventType == IncidentEventType.Escalated)
            .ToList();

        var escalated = Assert.Single(events);
        Assert.Contains("tier 1", escalated.Description);
        Assert.Contains("Payments Critical", escalated.Description);
    }

    // --- timing of later tiers ---

    [Fact]
    public async Task The_next_tier_waits_for_its_predecessors_timeout()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();
        var escalator = EscalatorFor(fixture, sender);
        var created = CreatedAtOf(fixture);

        await escalator.EscalateAsync(fixture.IncidentId, created, CancellationToken.None);
        var outcome = await escalator.EscalateAsync(
            fixture.IncidentId, created.AddMinutes(Tier1TimeoutMinutes - 1), CancellationToken.None);

        Assert.Equal(EscalationOutcome.NotYetDue, outcome);
        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task The_next_tier_fires_once_the_timeout_has_elapsed()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();
        var escalator = EscalatorFor(fixture, sender);
        var created = CreatedAtOf(fixture);

        await escalator.EscalateAsync(fixture.IncidentId, created, CancellationToken.None);
        var outcome = await escalator.EscalateAsync(
            fixture.IncidentId, created.AddMinutes(Tier1TimeoutMinutes + 1), CancellationToken.None);

        // Tier 2 is the on-call-only tier and there is no shift, so it reaches nobody.
        Assert.Equal(EscalationOutcome.ReachedNobody, outcome);
        Assert.Equal(2, sender.Sent.Count);
        Assert.Equal(2, sender.Sent[1].Level);
    }

    [Fact]
    public async Task Only_one_tier_fires_per_pass_however_overdue_the_incident_is()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();

        await EscalatorFor(fixture, sender)
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture).AddDays(1), CancellationToken.None);

        Assert.Single(sender.Sent);
    }

    // --- stop conditions ---

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Escalation_stops_once_somebody_takes_the_incident(bool alsoResolve)
    {
        using var fixture = new TestDb();
        var incident = fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId);
        incident.Acknowledge(fixture.ResponderId, "Dana Responder");
        if (alsoResolve) incident.Resolve("Rolled back.");
        fixture.Db.SaveChanges();

        var sender = new RecordingSender();
        var outcome = await EscalatorFor(fixture, sender)
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

        Assert.Equal(EscalationOutcome.Skipped, outcome);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task An_incident_on_a_service_with_no_policy_is_left_alone()
    {
        using var fixture = new TestDb();
        var orphan = Incident.Create(
            fixture.TenantId, fixture.SecondServiceId, "No policy here", IncidentSeverity.P3_Medium);
        fixture.Db.Add(orphan);
        fixture.Db.SaveChanges();

        var sender = new RecordingSender();
        var outcome = await EscalatorFor(fixture, sender)
            .EscalateAsync(orphan.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(EscalationOutcome.Skipped, outcome);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task An_unknown_incident_is_left_alone()
    {
        using var fixture = new TestDb();

        var outcome = await EscalatorFor(fixture, new RecordingSender())
            .EscalateAsync(Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(EscalationOutcome.Skipped, outcome);
    }

    // --- exhaustion ---

    [Fact]
    public async Task Running_out_of_tiers_is_recorded_on_the_incident()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();
        var escalator = EscalatorFor(fixture, sender);
        var created = CreatedAtOf(fixture);

        // Three tiers, then one pass beyond the last.
        await escalator.EscalateAsync(fixture.IncidentId, created, CancellationToken.None);
        await escalator.EscalateAsync(fixture.IncidentId, created.AddMinutes(10), CancellationToken.None);
        await escalator.EscalateAsync(fixture.IncidentId, created.AddMinutes(30), CancellationToken.None);
        var outcome = await escalator.EscalateAsync(
            fixture.IncidentId, created.AddMinutes(100), CancellationToken.None);

        Assert.Equal(EscalationOutcome.Exhausted, outcome);
        Assert.Equal(3, sender.Sent.Count);

        fixture.Db.ChangeTracker.Clear();
        var incident = fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId);
        Assert.NotNull(incident.EscalationExhaustedAt);
    }

    // The reason exhaustion is recorded at all: an exhausted incident must drop out of the
    // work entirely rather than being re-evaluated on every tick forever.
    [Fact]
    public async Task An_exhausted_incident_is_skipped_from_then_on()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();
        var escalator = EscalatorFor(fixture, sender);
        var created = CreatedAtOf(fixture);

        foreach (var minutes in new[] { 0, 10, 30, 100 })
            await escalator.EscalateAsync(fixture.IncidentId, created.AddMinutes(minutes), CancellationToken.None);

        var outcome = await escalator.EscalateAsync(
            fixture.IncidentId, created.AddMinutes(200), CancellationToken.None);

        Assert.Equal(EscalationOutcome.Skipped, outcome);
        Assert.Equal(3, sender.Sent.Count);
    }

    // --- coverage gaps ---

    [Fact]
    public async Task A_tier_that_resolves_to_nobody_is_still_recorded_and_still_advances()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender();
        var escalator = EscalatorFor(fixture, sender);
        var created = CreatedAtOf(fixture);

        await escalator.EscalateAsync(fixture.IncidentId, created, CancellationToken.None);
        var outcome = await escalator.EscalateAsync(
            fixture.IncidentId, created.AddMinutes(6), CancellationToken.None);

        Assert.Equal(EscalationOutcome.ReachedNobody, outcome);

        fixture.Db.ChangeTracker.Clear();
        var tier2 = fixture.Db.Set<IncidentEscalation>().Include(e => e.Targets).Single(e => e.Level == 2);

        // Stamped despite reaching nobody, or it would be retried forever.
        Assert.NotNull(tier2.NotifiedAt);
        Assert.Empty(tier2.Targets);
    }

    // --- send failures ---

    [Fact]
    public async Task A_failed_send_leaves_the_escalation_unstamped_for_the_next_pass()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender { ThrowOnSend = new InvalidOperationException("smtp down") };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EscalatorFor(fixture, sender)
                .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None));

        fixture.Db.ChangeTracker.Clear();
        var escalation = fixture.Db.Set<IncidentEscalation>().Single();

        // The attempt was committed before the send, which is what bounds the retry loop.
        Assert.Null(escalation.NotifiedAt);
        Assert.Equal(1, escalation.Attempts);
    }

    [Fact]
    public async Task A_recovered_sender_finishes_the_claim_rather_than_claiming_again()
    {
        using var fixture = new TestDb();
        var failing = new RecordingSender { ThrowOnSend = new InvalidOperationException("smtp down") };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EscalatorFor(fixture, failing)
                .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None));

        fixture.Db.ChangeTracker.Clear();
        var working = new RecordingSender();
        var outcome = await EscalatorFor(fixture, working)
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

        Assert.Equal(EscalationOutcome.Notified, outcome);
        Assert.Single(working.Sent);
        Assert.Equal(1, working.Sent[0].Level);

        fixture.Db.ChangeTracker.Clear();
        var escalation = fixture.Db.Set<IncidentEscalation>().Single();
        Assert.Equal(2, escalation.Attempts);
        Assert.NotNull(escalation.NotifiedAt);
    }

    // The bound the retry loop needed: without it, a transport that fails after delivering
    // would re-page the same people on every tick indefinitely.
    [Fact]
    public async Task Sending_gives_up_after_the_configured_number_of_attempts()
    {
        using var fixture = new TestDb();
        var sender = new RecordingSender { ThrowOnSend = new InvalidOperationException("smtp down") };
        var created = CreatedAtOf(fixture);

        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                EscalatorFor(fixture, sender, maxSendAttempts: 2)
                    .EscalateAsync(fixture.IncidentId, created, CancellationToken.None));
            fixture.Db.ChangeTracker.Clear();
        }

        var outcome = await EscalatorFor(fixture, sender, maxSendAttempts: 2)
            .EscalateAsync(fixture.IncidentId, created, CancellationToken.None);

        Assert.Equal(EscalationOutcome.GaveUp, outcome);
    }

    // A broken transport must not freeze the whole chain: the next tier still takes over
    // when its timeout elapses.
    [Fact]
    public async Task A_tier_that_gave_up_still_hands_over_to_the_next_one()
    {
        using var fixture = new TestDb();
        var created = CreatedAtOf(fixture);
        var failing = new RecordingSender { ThrowOnSend = new InvalidOperationException("smtp down") };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EscalatorFor(fixture, failing, maxSendAttempts: 1)
                .EscalateAsync(fixture.IncidentId, created, CancellationToken.None));
        fixture.Db.ChangeTracker.Clear();

        var working = new RecordingSender();
        var outcome = await EscalatorFor(fixture, working, maxSendAttempts: 1)
            .EscalateAsync(fixture.IncidentId, created.AddMinutes(6), CancellationToken.None);

        // Tier 1 was abandoned; tier 2 went out, reaching nobody since no shift exists.
        Assert.Equal(EscalationOutcome.ReachedNobody, outcome);
        Assert.Single(working.Sent);
        Assert.Equal(2, working.Sent[0].Level);
    }

    // --- multi-instance safety ---

    [Fact]
    public async Task A_tier_already_claimed_elsewhere_is_skipped_rather_than_paged_twice()
    {
        using var fixture = new TestDb();

        // Stands in for another instance winning the race to claim tier 1.
        fixture.Db.Add(IncidentEscalation.Claim(
            fixture.TenantId, fixture.IncidentId, fixture.MixedTierId, 1, CreatedAtOf(fixture)));
        fixture.Db.SaveChanges();

        var alreadyNotified = fixture.Db.Set<IncidentEscalation>().Single();
        alreadyNotified.MarkNotified([]);
        fixture.Db.SaveChanges();
        fixture.Db.ChangeTracker.Clear();

        var sender = new RecordingSender();
        var outcome = await EscalatorFor(fixture, sender)
            .EscalateAsync(fixture.IncidentId, CreatedAtOf(fixture), CancellationToken.None);

        // Tier 1 is spoken for and tier 2 is not due yet.
        Assert.Equal(EscalationOutcome.NotYetDue, outcome);
        Assert.Empty(sender.Sent);
    }

    // --- the tenant guard ---

    [Fact]
    public async Task The_escalator_refuses_to_run_without_a_tenant()
    {
        using var fixture = new TestDb();
        fixture.AsTenant(null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EscalatorFor(fixture, new RecordingSender())
                .EscalateAsync(fixture.IncidentId, DateTime.UtcNow, CancellationToken.None));

        Assert.Contains("tenant-scoped", ex.Message);
    }

    internal sealed class RecordingSender : INotificationSender
    {
        public List<EscalationNotification> Sent { get; } = [];
        public Exception? ThrowOnSend { get; set; }

        public Task SendAsync(EscalationNotification notification, CancellationToken ct)
        {
            if (ThrowOnSend is not null)
                throw ThrowOnSend;

            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }
}
