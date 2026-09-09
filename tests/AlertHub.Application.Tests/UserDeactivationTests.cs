using AlertHub.Application.Auth.Login;
using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Features.OnCall.CreateOnCallShift;
using AlertHub.Application.Features.Users;
using AlertHub.Application.Features.Users.GetUsers;
using AlertHub.Application.Features.Users.SetUserActivation;
using AlertHub.Application.Interfaces;
using AlertHub.Application.Settings;
using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;
using AlertHub.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Tests;

/// <summary>
/// Somebody leaving is recorded, not deleted, so the question these tests answer is whether
/// the record actually reaches the places that page people. A deactivation that shows in a
/// list but still wakes someone at 3am is worse than none at all.
/// </summary>
public class UserDeactivationTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OutsideAnyShift = End.AddDays(3);

    private static EscalationTargetResolver ResolverFor(TestDb fixture) =>
        new(fixture.Db, new OnCallResolver(fixture.Db));

    private static async Task Deactivate(TestDb fixture, Guid userId, Guid by) =>
        await SetUserActivationHandler.HandleAsync(
            new SetUserActivationCommand(userId, Active: false, by),
            fixture.Db, CancellationToken.None);

    private static Task<List<UserSummaryDto>> Users(TestDb fixture, bool includeDeactivated = false) =>
        GetUsersHandler.HandleAsync(
            new GetUsersQuery(includeDeactivated), fixture.Db, CancellationToken.None);

    [Fact]
    public async Task A_departed_person_is_not_swept_by_a_role_tier()
    {
        using var fixture = new TestDb();

        var before = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        await Deactivate(fixture, fixture.ResponderId, fixture.AdminId);

        var after = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        Assert.Contains(before, t => t.UserId == fixture.ResponderId);
        Assert.DoesNotContain(after, t => t.UserId == fixture.ResponderId);
        // Eli is still swept, so the tier is alive and the absence is about Dana alone.
        Assert.Contains(after, t => t.UserId == fixture.SecondResponderId);
    }

    [Fact]
    public async Task A_departed_person_named_directly_on_a_tier_is_not_a_target()
    {
        using var fixture = new TestDb();

        await Deactivate(fixture, fixture.AdminId, fixture.ResponderId);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.DirectUserTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        // That tier names Avery and nobody else, so it is now a tier that reaches no one —
        // the state the resolver exists to surface rather than silently succeed at.
        Assert.Empty(targets);
    }

    // A shift outliving the person on it would otherwise look like coverage.
    [Fact]
    public async Task A_departed_person_on_an_active_shift_resolves_to_nobody()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var midShift = Start.AddHours(3);

        var before = await new OnCallResolver(fixture.Db)
            .ResolveAsync(fixture.ServiceId, midShift, CancellationToken.None);

        await Deactivate(fixture, fixture.ResponderId, fixture.AdminId);

        var after = await new OnCallResolver(fixture.Db)
            .ResolveAsync(fixture.ServiceId, midShift, CancellationToken.None);

        Assert.Equal(fixture.ResponderId, before?.UserId);
        Assert.Null(after);
    }

    [Fact]
    public async Task A_departed_person_cannot_be_put_on_a_rota()
    {
        using var fixture = new TestDb();
        await Deactivate(fixture, fixture.ResponderId, fixture.AdminId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateOnCallShiftHandler.HandleAsync(
                new CreateOnCallShiftCommand(fixture.ServiceId, fixture.ResponderId, Start, End, null),
                fixture.Db, fixture.TenantContext, CancellationToken.None));
    }

    [Fact]
    public async Task Sign_in_is_refused_and_says_nothing_about_why()
    {
        using var fixture = new TestDb();
        var hasher = new PassThroughHasher();
        var user = fixture.AddUser(
            "leaver@acme.test", "Lee Leaver", PermissionRole.Responder, EscalationRole.Responder);
        SetPassword(fixture, user.Id, hasher.Hash("Sup3rSecret"));

        var before = await LoginHandler.HandleAsync(
            new LoginCommand("leaver@acme.test", "Sup3rSecret"),
            fixture.Db, TokenServiceFor(), hasher, CancellationToken.None);

        await Deactivate(fixture, user.Id, fixture.AdminId);

        var refused = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            LoginHandler.HandleAsync(
                new LoginCommand("leaver@acme.test", "Sup3rSecret"),
                fixture.Db, TokenServiceFor(), hasher, CancellationToken.None));

        Assert.NotNull(before.AccessToken);
        Assert.Equal("Invalid email or password.", refused.Message);
    }

    [Fact]
    public async Task Listing_hides_departed_people_by_default_and_shows_them_on_request()
    {
        using var fixture = new TestDb();
        await Deactivate(fixture, fixture.ResponderId, fixture.AdminId);

        var forAPicker = await Users(fixture);
        var forADashboard = await Users(fixture, includeDeactivated: true);

        Assert.DoesNotContain(forAPicker, u => u.Id == fixture.ResponderId);
        Assert.Equal(2, forAPicker.Count);

        var departed = Assert.Single(forADashboard, u => u.Id == fixture.ResponderId);
        Assert.NotNull(departed.DeactivatedAt);
    }

    // The footgun a dashboard makes easy: the only Admin clicks the row that is themselves.
    [Fact]
    public async Task You_cannot_deactivate_your_own_account()
    {
        using var fixture = new TestDb();

        var refused = await Assert.ThrowsAsync<ConflictException>(() =>
            Deactivate(fixture, fixture.AdminId, fixture.AdminId));

        var stillActive = await fixture.Db.Set<User>().SingleAsync(u => u.Id == fixture.AdminId);

        Assert.Contains("your own account", refused.Message);
        Assert.Null(stillActive.DeactivatedAt);
    }

    [Fact]
    public async Task Another_tenants_user_cannot_be_deactivated()
    {
        using var fixture = new TestDb();
        var outsider = await fixture.Db.Set<User>()
            .IgnoreQueryFilters()
            .SingleAsync(u => u.TenantId == fixture.OtherTenantId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Deactivate(fixture, outsider.Id, fixture.AdminId));
    }

    [Fact]
    public async Task Reactivating_puts_them_back_on_the_tier()
    {
        using var fixture = new TestDb();
        await Deactivate(fixture, fixture.ResponderId, fixture.AdminId);

        await SetUserActivationHandler.HandleAsync(
            new SetUserActivationCommand(fixture.ResponderId, Active: true, fixture.AdminId),
            fixture.Db, CancellationToken.None);

        var targets = await ResolverFor(fixture).ResolveTierTargetsAsync(
            fixture.MixedTierId, fixture.ServiceId, OutsideAnyShift, CancellationToken.None);

        Assert.Contains(targets, t => t.UserId == fixture.ResponderId);
    }

    private static void SetPassword(TestDb fixture, Guid userId, string hash)
    {
        // PasswordHash has no setter by design; the fixture creates users with a placeholder,
        // so the sign-in tests write the real hash the way a seed script would.
        fixture.Db.Database.ExecuteSqlRaw(
            "UPDATE Users SET PasswordHash = {0} WHERE Id = {1}", hash, userId);
        fixture.Db.ChangeTracker.Clear();
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

    private sealed class PassThroughHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }
}
