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
    // responder rota, and an Admin-targeting tier must leave them alone.
    [Fact]
    public async Task An_admin_who_pages_as_a_responder_is_not_swept_by_an_admin_tier()
    {
        using var fixture = new TestDb();
        var platformAdmin = fixture.AddUser(
            "priya@acme.test", "Priya Platform", PermissionRole.Admin, EscalationRole.Responder);
        AlsoPageRole(fixture, EscalationRole.Admin);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        // Avery, whose escalation role really is Admin, is still reached — so the sweep ran
        // and the absence below is about Priya's escalation role, not a dead tier.
        Assert.Contains(targets, t => t.UserId == fixture.AdminId);
        Assert.DoesNotContain(targets, t => t.UserId == platformAdmin.Id);
    }

    // And the other direction: carrying a pager is not a grant of authority.
    [Fact]
    public async Task Someone_paged_as_an_admin_need_not_hold_admin_permissions()
    {
        using var fixture = new TestDb();
        var support = fixture.AddUser(
            "sam@acme.test", "Sam Support", PermissionRole.Viewer, EscalationRole.Admin);
        AlsoPageRole(fixture, EscalationRole.Admin);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.OnCallOnlyTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        Assert.Contains(targets, t => t.UserId == support.Id);
        Assert.Equal(PermissionRole.Viewer, support.PermissionRole);
    }

    [Fact]
    public async Task A_role_sweep_reads_the_escalation_role_and_ignores_the_permission_role()
    {
        using var fixture = new TestDb();
        var viewerOnPager = fixture.AddUser(
            "quinn@acme.test", "Quinn Quiet", PermissionRole.Viewer, EscalationRole.Responder);

        // MixedTier pages the Responder escalation role.
        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        Assert.Contains(targets, t => t.UserId == viewerOnPager.Id);
        Assert.All(targets, t => Assert.Equal(EscalationTargetSource.Role, t.Source));
    }

    // The claim behind every RequireRole check. If this carried the escalation role, putting
    // somebody on the admin rota would hand them the admin endpoints.
    [Fact]
    public void The_role_claim_carries_the_permission_role()
    {
        var user = User.Create(
            Guid.NewGuid(), "sam@acme.test", "Sam Support", "hash",
            PermissionRole.Viewer, EscalationRole.Admin);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(TokenServiceFor().GenerateAccessToken(user));

        Assert.Equal("Viewer", token.Claims.Single(c => c.Type == "role").Value);
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
