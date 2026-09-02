using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Tests;

public class OnCallShiftTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Service = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static TimeWindow AWindow() => TimeWindow.Create(Start, Start.AddDays(1));

    [Fact]
    public void Create_assigns_identity_and_stamps_creation()
    {
        var before = DateTime.UtcNow;

        var shift = OnCallShift.Create(Tenant, Service, User, AWindow(), "Asia/Jerusalem");

        Assert.NotEqual(Guid.Empty, shift.Id);
        Assert.Equal(Tenant, shift.TenantId);
        Assert.Equal(Service, shift.ServiceId);
        Assert.Equal(User, shift.UserId);
        Assert.Equal("Asia/Jerusalem", shift.TimeZoneId);
        Assert.InRange(shift.CreatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void Create_leaves_the_zone_unset_when_not_supplied()
    {
        var shift = OnCallShift.Create(Tenant, Service, User, AWindow());

        Assert.Null(shift.TimeZoneId);
    }

    [Fact]
    public void Reassign_changes_the_accountable_person()
    {
        var shift = OnCallShift.Create(Tenant, Service, User, AWindow());
        var replacement = Guid.NewGuid();

        shift.Reassign(replacement);

        Assert.Equal(replacement, shift.UserId);
    }

    // A shift with nobody on it is not a shift. Guarding here rather than only in the
    // validator means the invariant holds however the entity is reached.
    [Fact]
    public void Reassign_rejects_an_empty_user()
    {
        var shift = OnCallShift.Create(Tenant, Service, User, AWindow());

        var ex = Assert.Throws<DomainException>(() => shift.Reassign(Guid.Empty));
        Assert.Contains("assigned to a user", ex.Message);
    }

    [Fact]
    public void Reschedule_replaces_the_window_and_its_zone()
    {
        var shift = OnCallShift.Create(Tenant, Service, User, AWindow(), "Asia/Jerusalem");
        var moved = TimeWindow.Create(Start.AddDays(3), Start.AddDays(4));

        shift.Reschedule(moved, "Europe/London");

        Assert.Equal(Start.AddDays(3), shift.Window.Start);
        Assert.Equal("Europe/London", shift.TimeZoneId);
    }

    // The zone describes how the window was authored, so re-authoring without one
    // clears it rather than leaving a stale zone attached to new dates.
    [Fact]
    public void Reschedule_without_a_zone_clears_the_previous_one()
    {
        var shift = OnCallShift.Create(Tenant, Service, User, AWindow(), "Asia/Jerusalem");

        shift.Reschedule(TimeWindow.Create(Start.AddDays(3), Start.AddDays(4)));

        Assert.Null(shift.TimeZoneId);
    }
}
