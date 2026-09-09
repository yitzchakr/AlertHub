using AlertHub.Domain.Entities;

namespace AlertHub.Domain.Tests;

public class UserTests
{
    private static readonly DateTime Departure = new(2027, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    private static User AUser() => User.Create(
        Guid.NewGuid(), "dana@acme.test", "Dana Responder", "hash",
        PermissionRole.Responder, EscalationRole.Responder);

    [Fact]
    public void A_new_user_is_active()
    {
        Assert.Null(AUser().DeactivatedAt);
    }

    [Fact]
    public void Deactivation_records_when_it_happened()
    {
        var user = AUser();

        user.Deactivate(Departure);

        Assert.Equal(Departure, user.DeactivatedAt);
    }

    // Without this, a departed account keeps minting access tokens for the refresh token's
    // remaining week — the deactivation would show in the UI and mean nothing at the door.
    [Fact]
    public void Deactivation_revokes_the_refresh_token()
    {
        var user = AUser();
        user.SetRefreshToken("still-valid", Departure.AddDays(7));

        user.Deactivate(Departure);

        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiryTime);
    }

    // Deactivating twice must not restamp the date: when somebody left is a fact, and the
    // second call is usually a double-click.
    [Fact]
    public void Deactivating_an_already_departed_user_does_not_move_the_date()
    {
        var user = AUser();
        user.Deactivate(Departure);

        user.Deactivate(Departure.AddDays(30));

        Assert.Equal(Departure, user.DeactivatedAt);
    }

    [Fact]
    public void Reactivation_puts_them_back()
    {
        var user = AUser();
        user.Deactivate(Departure);

        user.Reactivate();

        Assert.Null(user.DeactivatedAt);
    }

    // Coming back does not restore the old session — they sign in again.
    [Fact]
    public void Reactivation_does_not_restore_the_revoked_token()
    {
        var user = AUser();
        user.SetRefreshToken("old-token", Departure.AddDays(7));
        user.Deactivate(Departure);

        user.Reactivate();

        Assert.Null(user.RefreshToken);
    }
}
