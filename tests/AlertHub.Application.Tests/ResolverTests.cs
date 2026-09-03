using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.OnCall;
using AlertHub.Domain.Entities;

namespace AlertHub.Application.Tests;

public class OnCallResolverTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Resolves_the_person_accountable_mid_shift()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var resolver = new OnCallResolver(fixture.Db);

        var onCall = await resolver.ResolveAsync(
            fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        Assert.NotNull(onCall);
        Assert.Equal(fixture.ResponderId, onCall.UserId);
        Assert.Equal("Dana Responder", onCall.DisplayName);
        Assert.Equal(Start, onCall.ShiftStartUtc);
    }

    // The handoff instant belongs to the incoming shift. Getting this backwards pages the
    // person who just went off duty.
    [Fact]
    public async Task At_the_handoff_instant_the_incoming_shift_owns_it()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        fixture.AddShift(fixture.ServiceId, fixture.SecondResponderId, End, End.AddDays(1));
        var resolver = new OnCallResolver(fixture.Db);

        var onCall = await resolver.ResolveAsync(fixture.ServiceId, End, CancellationToken.None);

        Assert.Equal(fixture.SecondResponderId, onCall!.UserId);
    }

    [Fact]
    public async Task An_unstaffed_instant_resolves_to_nobody()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var resolver = new OnCallResolver(fixture.Db);

        var covered = await resolver.ResolveAsync(
            fixture.ServiceId, Start.AddHours(3), CancellationToken.None);
        var gap = await resolver.ResolveAsync(
            fixture.ServiceId, End.AddDays(3), CancellationToken.None);

        // Paired deliberately: the null is only meaningful alongside a hit proving the
        // shift exists and is reachable.
        Assert.NotNull(covered);
        Assert.Null(gap);
    }

    [Fact]
    public async Task A_shift_on_another_service_does_not_answer_for_this_one()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.SecondServiceId, fixture.ResponderId, Start, End);
        var resolver = new OnCallResolver(fixture.Db);

        var onCall = await resolver.ResolveAsync(
            fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        Assert.Null(onCall);
    }

    [Fact]
    public async Task Another_tenant_cannot_see_who_is_on_call()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var resolver = new OnCallResolver(fixture.Db);

        var own = await resolver.ResolveAsync(
            fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        fixture.AsTenant(fixture.OtherTenantId);
        var foreign = await resolver.ResolveAsync(
            fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        Assert.NotNull(own);
        Assert.Null(foreign);
    }

    [Fact]
    public async Task An_offsetless_instant_is_read_as_utc()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var resolver = new OnCallResolver(fixture.Db);

        var naive = DateTime.SpecifyKind(Start.AddHours(3), DateTimeKind.Unspecified);
        var onCall = await resolver.ResolveAsync(fixture.ServiceId, naive, CancellationToken.None);

        Assert.NotNull(onCall);
    }
}

public class EscalationTargetResolverTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    private static EscalationTargetResolver ResolverFor(TestDb fixture) =>
        new(fixture.Db, new OnCallResolver(fixture.Db));

    [Fact]
    public async Task A_tier_reaches_both_its_on_call_and_role_targets()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.AdminId, Start, End);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        // Both responders via the role sweep, plus the admin who is actually on call.
        Assert.Equal(3, targets.Count);
        Assert.Contains(targets, t =>
            t.UserId == fixture.AdminId && t.Source == EscalationTargetSource.OnCall);
        Assert.Contains(targets, t =>
            t.UserId == fixture.ResponderId && t.Source == EscalationTargetSource.Role);
    }

    // This is the loop the module closed: before it, a UseOnCallSchedule assignment
    // reached nobody at all, regardless of coverage.
    [Fact]
    public async Task An_on_call_only_tier_reaches_the_person_on_duty()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        var target = Assert.Single(targets);
        Assert.Equal(fixture.ResponderId, target.UserId);
        Assert.Equal(EscalationTargetSource.OnCall, target.Source);
    }

    [Fact]
    public async Task An_on_call_only_tier_reaches_nobody_during_a_coverage_gap()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var resolver = ResolverFor(fixture);

        var covered = await resolver.ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, Start.AddHours(3), CancellationToken.None);
        var inGap = await resolver.ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, End.AddDays(3), CancellationToken.None);

        // The empty result is asserted next to a populated one, so it cannot pass because
        // the tier was unreachable for some unrelated reason.
        Assert.Single(covered);
        Assert.Empty(inGap);
    }

    [Fact]
    public async Task A_gap_removes_only_the_on_call_target_and_leaves_the_role_sweep()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.AdminId, Start, End);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, End.AddDays(3), CancellationToken.None);

        Assert.Equal(2, targets.Count);
        Assert.All(targets, t => Assert.Equal(EscalationTargetSource.Role, t.Source));
    }

    // A person reachable two ways appears once, attributed to the more specific source.
    [Fact]
    public async Task A_person_named_directly_and_on_call_is_listed_once_as_a_direct_target()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.AdminId, Start, End);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.DirectUserTierId, fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        var target = Assert.Single(targets);
        Assert.Equal(fixture.AdminId, target.UserId);
        Assert.Equal(EscalationTargetSource.DirectUser, target.Source);
    }

    [Fact]
    public async Task A_role_sweep_never_crosses_the_tenant_boundary()
    {
        using var fixture = new TestDb();

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, End.AddDays(3), CancellationToken.None);

        // Globex has a Responder too; only Acme's two may appear.
        Assert.Equal(2, targets.Count);
        Assert.DoesNotContain(targets, t => t.DisplayName == "Zoe Outsider");
    }

    [Fact]
    public async Task An_unknown_tier_reaches_nobody()
    {
        using var fixture = new TestDb();

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            Guid.NewGuid(), fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        Assert.Empty(targets);
    }
}
