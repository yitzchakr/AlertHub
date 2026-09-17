using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Settings;
using AlertHub.Domain.Entities;
using AlertHub.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace AlertHub.Application.Tests;

/// <summary>
/// PermissionRole and EscalationRole were one field, so granting somebody Admin to let them
/// edit a policy also added them to every tier that paged Admin — a change to who gets woken
/// up, made through a screen that said nothing about paging. These tests hold the two apart
/// in both directions: authorization must not move paging, and paging must not confer
/// authorization.
///
/// The two enums no longer share a single value name, which makes the confusion hard to
/// restate. That is the point; these tests guard the behaviour the vocabulary now advertises.
/// </summary>
public class RoleSplitTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>A gap instant, so only the role sweep contributes targets.</summary>
    private static readonly DateTime OutsideAnyShift = End.AddDays(3);

    private static EscalationTargetResolver ResolverFor(TestDb fixture) =>
        new(fixture.Db, new OnCallResolver(fixture.Db));

    /// <summary>Points the on-call-only tier at an escalation role as well.</summary>
    private static void AlsoPageRole(TestDb fixture, EscalationRole role)
    {
        fixture.Db.Add(EscalationTierAssignment.ForRole(fixture.OnCallOnlyTierId, fixture.TenantId, role));
        fixture.Db.SaveChanges();
    }

    // The bug the split exists to remove: this person administers AlertHub but answers the
    // responder rota, and a Manager-targeting tier must leave them alone.
    [Fact]
    public async Task An_admin_who_pages_as_a_responder_is_not_swept_by_a_manager_tier()
    {
        using var fixture = new TestDb();
        var platformAdmin = fixture.AddUser(
            "priya@acme.test", "Priya Platform", PermissionRole.Admin, EscalationRole.Responder);
        AlsoPageRole(fixture, EscalationRole.Manager);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        // Avery, whose escalation role really is Manager, is still reached — so the sweep ran
        // and the absence below is about Priya's escalation role, not a dead tier.
        Assert.Contains(targets, t => t.UserId == fixture.AdminId);
        Assert.DoesNotContain(targets, t => t.UserId == platformAdmin.Id);
    }

    // And the other direction: carrying a pager is not a grant of authority.
    [Fact]
    public async Task Someone_paged_as_a_manager_need_not_hold_admin_permissions()
    {
        using var fixture = new TestDb();
        var support = fixture.AddUser(
            "sam@acme.test", "Sam Support", PermissionRole.Member, EscalationRole.Manager);
        AlsoPageRole(fixture, EscalationRole.Manager);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        Assert.Contains(targets, t => t.UserId == support.Id);
        Assert.Equal(PermissionRole.Member, support.PermissionRole);
    }

    [Fact]
    public async Task A_role_sweep_reads_the_escalation_role_and_ignores_the_permission_role()
    {
        using var fixture = new TestDb();
        var memberOnPager = fixture.AddUser(
            "quinn@acme.test", "Quinn Quiet", PermissionRole.Member, EscalationRole.Responder);

        // MixedTier pages the Responder escalation role.
        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        Assert.Contains(targets, t => t.UserId == memberOnPager.Id);
        Assert.All(targets, t => Assert.Equal(EscalationTargetSource.Role, t.Source));
    }

    // The claim behind every RequireRole check. If this carried the escalation role, putting
    // somebody on the manager rota would hand them the admin endpoints.
    [Fact]
    public void The_role_claim_carries_the_permission_role()
    {
        var user = User.Create(
            Guid.NewGuid(), "sam@acme.test", "Sam Support", "hash",
            PermissionRole.Member, EscalationRole.Manager);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(TokenServiceFor().GenerateAccessToken(user));

        Assert.Equal("Member", token.Claims.Single(c => c.Type == "role").Value);
    }

    // A null escalation role is the way to say "do not page this person for what they are".
    // It has to hold against every sweep on a tier, not just the one being tested.
    [Fact]
    public async Task Somebody_with_no_escalation_role_answers_no_role_sweep()
    {
        using var fixture = new TestDb();
        var unpaged = fixture.AddUser(
            "nina@acme.test", "Nina Nopage", PermissionRole.Admin, null);
        AlsoPageRole(fixture, EscalationRole.Manager);
        AlsoPageRole(fixture, EscalationRole.Responder);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        // Both sweeps demonstrably ran — Avery answers Manager, Dana answers Responder — so
        // Nina's absence is her null role rather than a tier that reached nobody.
        Assert.Contains(targets, t => t.UserId == fixture.AdminId);
        Assert.Contains(targets, t => t.UserId == fixture.ResponderId);
        Assert.DoesNotContain(targets, t => t.UserId == unpaged.Id);
    }

    // The half that makes "none" safe to choose: it excuses somebody from role sweeps, not
    // from being paged. Reading it as "never woken" would be a dangerous thing to believe.
    [Fact]
    public async Task Somebody_with_no_escalation_role_is_still_paged_when_named_directly()
    {
        using var fixture = new TestDb();
        var unpaged = fixture.AddUser(
            "nina@acme.test", "Nina Nopage", PermissionRole.Admin, null);
        fixture.Db.Add(EscalationTierAssignment.ForUser(
            fixture.OnCallOnlyTierId, fixture.TenantId, unpaged.Id));
        fixture.Db.SaveChanges();

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        var target = Assert.Single(targets);
        Assert.Equal(unpaged.Id, target.UserId);
        Assert.Equal(EscalationTargetSource.DirectUser, target.Source);
    }

    [Fact]
    public async Task Somebody_with_no_escalation_role_is_still_paged_when_on_call()
    {
        using var fixture = new TestDb();
        var unpaged = fixture.AddUser(
            "nina@acme.test", "Nina Nopage", PermissionRole.Admin, null);
        fixture.AddShift(fixture.ServiceId, unpaged.Id, Start, End);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, Start.AddHours(3), CancellationToken.None);

        var target = Assert.Single(targets);
        Assert.Equal(unpaged.Id, target.UserId);
        Assert.Equal(EscalationTargetSource.OnCall, target.Source);
    }

    private static TokenService TokenServiceFor() =>
        new(Options.Create(new JwtSettings
        {
            SecretKey = new string('k', 64),
            Issuer = "AlertHub",
            Audience = "AlertHub",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        }));
}
