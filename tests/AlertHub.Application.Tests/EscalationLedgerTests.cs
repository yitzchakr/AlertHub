using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

/// <summary>
/// The ledger's constraints are the multi-instance safety story, so they are asserted
/// against a real database rather than trusted from the configuration.
/// </summary>
public class EscalationLedgerTests
{
    private static readonly DateTime Due = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static IncidentEscalation Claim(TestDb fixture, Guid tierId, int level) =>
        IncidentEscalation.Claim(fixture.TenantId, fixture.IncidentId, tierId, level, Due);

    [Fact]
    public void A_claim_round_trips_with_its_recipients()
    {
        using var fixture = new TestDb();

        var escalation = Claim(fixture, fixture.MixedTierId, 1);
        escalation.MarkNotified(
        [
            new EscalationRecipient(fixture.ResponderId, EscalationTargetSource.OnCall),
            new EscalationRecipient(fixture.AdminId, EscalationTargetSource.Role),
        ]);
        fixture.Db.Add(escalation);
        fixture.Db.SaveChanges();

        fixture.Db.ChangeTracker.Clear();
        var stored = fixture.Db.Set<IncidentEscalation>()
            .Include(e => e.Targets)
            .Single();

        Assert.Equal(1, stored.Level);
        Assert.NotNull(stored.NotifiedAt);
        Assert.Equal(DateTimeKind.Utc, stored.DueAt.Kind);
        Assert.Equal(2, stored.Targets.Count);
        Assert.Contains(stored.Targets, t => t.Source == EscalationTargetSource.OnCall);
    }

    // The guarantee the whole design rests on: two API instances both decide a tier is
    // due, and only one of them gets to page anybody.
    [Fact]
    public void The_same_tier_cannot_be_claimed_twice_for_one_incident()
    {
        using var fixture = new TestDb();

        fixture.Db.Add(Claim(fixture, fixture.MixedTierId, 1));
        fixture.Db.SaveChanges();

        fixture.Db.Add(Claim(fixture, fixture.MixedTierId, 1));

        Assert.Throws<DbUpdateException>(() => fixture.Db.SaveChanges());
    }

    [Fact]
    public void Successive_tiers_can_each_be_claimed()
    {
        using var fixture = new TestDb();

        fixture.Db.Add(Claim(fixture, fixture.MixedTierId, 1));
        fixture.Db.SaveChanges();
        fixture.Db.Add(Claim(fixture, fixture.OnCallOnlyTierId, 2));
        fixture.Db.SaveChanges();

        Assert.Equal(2, fixture.Db.Set<IncidentEscalation>().Count());
    }

    [Fact]
    public void One_person_cannot_be_recorded_twice_for_the_same_escalation()
    {
        using var fixture = new TestDb();

        var escalation = Claim(fixture, fixture.MixedTierId, 1);
        escalation.MarkNotified([new EscalationRecipient(fixture.ResponderId, EscalationTargetSource.Role)]);
        fixture.Db.Add(escalation);
        fixture.Db.SaveChanges();

        fixture.Db.Add(IncidentEscalationTarget.Create(
            escalation.Id, fixture.TenantId, fixture.ResponderId, EscalationTargetSource.OnCall));

        Assert.Throws<DbUpdateException>(() => fixture.Db.SaveChanges());
    }

    [Fact]
    public void The_same_person_can_be_recorded_across_different_escalations()
    {
        using var fixture = new TestDb();

        foreach (var (tierId, level) in new[] { (fixture.MixedTierId, 1), (fixture.OnCallOnlyTierId, 2) })
        {
            var escalation = Claim(fixture, tierId, level);
            escalation.MarkNotified([new EscalationRecipient(fixture.ResponderId, EscalationTargetSource.OnCall)]);
            fixture.Db.Add(escalation);
            fixture.Db.SaveChanges();
        }

        Assert.Equal(2, fixture.Db.Set<IncidentEscalationTarget>().Count());
    }

    [Fact]
    public void Deleting_an_incident_takes_its_escalations_and_recipients_with_it()
    {
        using var fixture = new TestDb();

        var escalation = Claim(fixture, fixture.MixedTierId, 1);
        escalation.MarkNotified([new EscalationRecipient(fixture.ResponderId, EscalationTargetSource.Role)]);
        fixture.Db.Add(escalation);
        fixture.Db.SaveChanges();

        fixture.Db.Remove(fixture.Db.Set<Incident>().Single(i => i.Id == fixture.IncidentId));
        fixture.Db.SaveChanges();

        Assert.Equal(0, fixture.Db.Set<IncidentEscalation>().Count());
        Assert.Equal(0, fixture.Db.Set<IncidentEscalationTarget>().Count());
    }

    [Fact]
    public void Another_tenant_cannot_read_the_ledger()
    {
        using var fixture = new TestDb();

        fixture.Db.Add(Claim(fixture, fixture.MixedTierId, 1));
        fixture.Db.SaveChanges();

        var own = fixture.Db.Set<IncidentEscalation>().Count();

        fixture.AsTenant(fixture.OtherTenantId);
        var foreign = fixture.Db.Set<IncidentEscalation>().Count();

        Assert.Equal(1, own);
        Assert.Equal(0, foreign);
    }

    // Claimed-but-unsent is what the retry pass looks for, and the index serving that
    // query is filtered, so the query has to be shaped to match it.
    [Fact]
    public void Unsent_claims_are_findable_and_sent_ones_are_not()
    {
        using var fixture = new TestDb();

        fixture.Db.Add(Claim(fixture, fixture.MixedTierId, 1));

        var sent = Claim(fixture, fixture.OnCallOnlyTierId, 2);
        sent.MarkNotified([]);
        fixture.Db.Add(sent);
        fixture.Db.SaveChanges();

        var unsent = fixture.Db.Set<IncidentEscalation>()
            .Where(e => e.NotifiedAt == null)
            .ToList();

        Assert.Single(unsent);
        Assert.Equal(1, unsent[0].Level);
    }
}
