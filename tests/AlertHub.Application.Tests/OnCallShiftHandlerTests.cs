using AlertHub.Application.Features.OnCall.CreateOnCallShift;
using AlertHub.Application.Features.OnCall.DeleteOnCallShift;
using AlertHub.Application.Features.OnCall.UpdateOnCallShift;
using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

public class OnCallShiftHandlerTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    private static Task<CreateOnCallShiftResponse> Create(
        TestDb fixture, Guid serviceId, Guid userId, DateTime startUtc, DateTime endUtc,
        string? timeZoneId = null) =>
        CreateOnCallShiftHandler.HandleAsync(
            new CreateOnCallShiftCommand(serviceId, userId, startUtc, endUtc, timeZoneId),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

    [Fact]
    public async Task Create_persists_a_shift_with_a_normalized_window()
    {
        using var fixture = new TestDb();

        var response = await Create(
            fixture, fixture.ServiceId, fixture.ResponderId, Start, End, "Asia/Jerusalem");

        var stored = await fixture.Db.Set<OnCallShift>().SingleAsync();
        Assert.Equal(response.Id, stored.Id);
        Assert.Equal(fixture.TenantId, stored.TenantId);
        Assert.Equal("Asia/Jerusalem", stored.TimeZoneId);
        Assert.Equal(DateTimeKind.Utc, stored.Window.Start.Kind);
    }

    // The rule the module exists to hold: two people on call means nobody is accountable.
    [Fact]
    public async Task Create_rejects_a_window_overlapping_an_existing_shift()
    {
        using var fixture = new TestDb();
        await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => Create(
            fixture, fixture.ServiceId, fixture.SecondResponderId,
            Start.AddHours(12), End.AddHours(12)));

        Assert.Contains("overlaps", ex.Message);
        Assert.Equal(1, await fixture.Db.Set<OnCallShift>().CountAsync());
    }

    [Fact]
    public async Task Create_allows_a_shift_starting_exactly_when_another_ends()
    {
        using var fixture = new TestDb();
        await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        await Create(fixture, fixture.ServiceId, fixture.SecondResponderId, End, End.AddDays(1));

        Assert.Equal(2, await fixture.Db.Set<OnCallShift>().CountAsync());
    }

    [Fact]
    public async Task Create_allows_concurrent_shifts_on_different_services()
    {
        using var fixture = new TestDb();
        await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        await Create(fixture, fixture.SecondServiceId, fixture.SecondResponderId, Start, End);

        Assert.Equal(2, await fixture.Db.Set<OnCallShift>().CountAsync());
    }

    [Fact]
    public async Task Create_rejects_an_unknown_service()
    {
        using var fixture = new TestDb();

        await Assert.ThrowsAsync<NotFoundException>(() => Create(
            fixture, Guid.NewGuid(), fixture.ResponderId, Start, End));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_user()
    {
        using var fixture = new TestDb();

        await Assert.ThrowsAsync<NotFoundException>(() => Create(
            fixture, fixture.ServiceId, Guid.NewGuid(), Start, End));
    }

    // Another tenant's service must read as absent, not as forbidden: telling the caller
    // the id is real but off-limits would itself leak across the boundary.
    [Fact]
    public async Task Create_treats_another_tenants_service_as_absent()
    {
        using var fixture = new TestDb();

        await Assert.ThrowsAsync<NotFoundException>(() => Create(
            fixture, fixture.OtherTenantServiceId, fixture.ResponderId, Start, End));
    }

    [Fact]
    public async Task Create_rejects_an_inverted_window_from_the_domain()
    {
        using var fixture = new TestDb();

        await Assert.ThrowsAsync<DomainException>(() => Create(
            fixture, fixture.ServiceId, fixture.ResponderId, End, Start));
    }

    [Fact]
    public async Task Update_reassigns_and_moves_a_shift()
    {
        using var fixture = new TestDb();
        var created = await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        await UpdateOnCallShiftHandler.HandleAsync(
            new UpdateOnCallShiftCommand(
                created.Id, fixture.SecondResponderId, Start.AddDays(5), Start.AddDays(6)),
            fixture.Db, CancellationToken.None);

        var stored = await fixture.Db.Set<OnCallShift>().SingleAsync();
        Assert.Equal(fixture.SecondResponderId, stored.UserId);
        Assert.Equal(Start.AddDays(5), stored.Window.Start);
    }

    // Without excluding itself, any edit that kept a shift in roughly the same slot would
    // be rejected as clashing with its own stored row.
    [Fact]
    public async Task Update_does_not_treat_a_shift_as_clashing_with_itself()
    {
        using var fixture = new TestDb();
        var created = await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        await UpdateOnCallShiftHandler.HandleAsync(
            new UpdateOnCallShiftCommand(
                created.Id, fixture.ResponderId, Start.AddHours(1), End.AddHours(1)),
            fixture.Db, CancellationToken.None);

        var stored = await fixture.Db.Set<OnCallShift>().SingleAsync();
        Assert.Equal(Start.AddHours(1), stored.Window.Start);
    }

    [Fact]
    public async Task Update_still_rejects_a_clash_with_a_different_shift()
    {
        using var fixture = new TestDb();
        var first = await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);
        await Create(fixture, fixture.ServiceId, fixture.SecondResponderId, End, End.AddDays(1));

        await Assert.ThrowsAsync<ConflictException>(() =>
            UpdateOnCallShiftHandler.HandleAsync(
                new UpdateOnCallShiftCommand(
                    first.Id, fixture.ResponderId, Start, End.AddHours(6)),
                fixture.Db, CancellationToken.None));
    }

    [Fact]
    public async Task Update_rejects_an_unknown_shift()
    {
        using var fixture = new TestDb();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            UpdateOnCallShiftHandler.HandleAsync(
                new UpdateOnCallShiftCommand(Guid.NewGuid(), fixture.ResponderId, Start, End),
                fixture.Db, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_removes_the_shift_and_reports_success()
    {
        using var fixture = new TestDb();
        var created = await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        var deleted = await DeleteOnCallShiftHandler.HandleAsync(
            new DeleteOnCallShiftCommand(created.Id), fixture.Db, CancellationToken.None);

        Assert.True(deleted);
        Assert.Equal(0, await fixture.Db.Set<OnCallShift>().CountAsync());
    }

    [Fact]
    public async Task Delete_reports_failure_for_an_unknown_shift()
    {
        using var fixture = new TestDb();

        var deleted = await DeleteOnCallShiftHandler.HandleAsync(
            new DeleteOnCallShiftCommand(Guid.NewGuid()), fixture.Db, CancellationToken.None);

        Assert.False(deleted);
    }

    [Fact]
    public async Task Delete_cannot_reach_another_tenants_shift()
    {
        using var fixture = new TestDb();
        var created = await Create(fixture, fixture.ServiceId, fixture.ResponderId, Start, End);

        fixture.AsTenant(fixture.OtherTenantId);
        var deleted = await DeleteOnCallShiftHandler.HandleAsync(
            new DeleteOnCallShiftCommand(created.Id), fixture.Db, CancellationToken.None);

        Assert.False(deleted);

        // The row survived, so the false above is the query filter refusing to see it
        // rather than a delete that happened to find nothing.
        fixture.AsTenant(fixture.TenantId);
        Assert.Equal(1, await fixture.Db.Set<OnCallShift>().CountAsync());
    }
}
