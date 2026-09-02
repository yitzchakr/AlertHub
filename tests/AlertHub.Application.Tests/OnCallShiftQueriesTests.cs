using AlertHub.Application.Features.OnCall;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

/// <summary>
/// The shift rules exist in two representations: TimeWindow states them for in-memory
/// values, OnCallShiftQueries restates them as SQL predicates. These tests assert the two
/// agree, because a closed/half-open mix-up between them would surface only as the wrong
/// person being paged at a handoff.
/// </summary>
public class OnCallShiftQueriesTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ActiveAt_agrees_with_Contains_across_the_boundaries()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);
        var window = TimeWindow.Create(Start, End);

        DateTime[] probes =
        [
            Start.AddDays(-1),
            Start.AddTicks(-1),
            Start,
            Start.AddTicks(1),
            Start.AddHours(12),
            End.AddTicks(-1),
            End,
            End.AddTicks(1),
            End.AddDays(1),
        ];

        foreach (var probe in probes)
        {
            var inMemory = window.Contains(probe);
            var viaSql = await fixture.Db.Set<OnCallShift>()
                .ActiveAt(fixture.ServiceId, probe)
                .AnyAsync();

            Assert.Equal(inMemory, viaSql);
        }
    }

    // Hours are relative to a shift occupying [0, 24). The final flag is what both
    // representations must agree on.
    [Theory]
    [InlineData(-48, -24, false)] // entirely before
    [InlineData(-24, 0, false)]   // ends exactly as the shift starts: a clean handoff
    [InlineData(-12, 12, true)]   // straddles the start
    [InlineData(2, 4, true)]      // wholly inside
    [InlineData(0, 24, true)]     // identical
    [InlineData(12, 36, true)]    // straddles the end
    [InlineData(24, 48, false)]   // starts exactly as the shift ends: a clean handoff
    [InlineData(48, 72, false)]   // entirely after
    public async Task OverlappingService_agrees_with_OverlapsWith(
        int fromHours, int toHours, bool expectedToOverlap)
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);

        var candidate = TimeWindow.Create(Start.AddHours(fromHours), Start.AddHours(toHours));
        var existing = TimeWindow.Create(Start, End);

        var inMemory = existing.OverlapsWith(candidate);
        var viaSql = await fixture.Db.Set<OnCallShift>()
            .OverlappingService(fixture.ServiceId, candidate.Start, candidate.End)
            .AnyAsync();

        Assert.Equal(expectedToOverlap, inMemory);
        Assert.Equal(expectedToOverlap, viaSql);
    }

    [Fact]
    public async Task OverlappingService_ignores_shifts_on_other_services()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.SecondServiceId, fixture.ResponderId, Start, End);

        var clashing = await fixture.Db.Set<OnCallShift>()
            .OverlappingService(fixture.ServiceId, Start, End)
            .AnyAsync();

        Assert.False(clashing);
    }

    [Fact]
    public async Task IntersectingRange_leaves_an_omitted_bound_unbounded()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);

        var noBounds = await fixture.Db.Set<OnCallShift>()
            .IntersectingRange(null, null)
            .CountAsync();
        var fromOnly = await fixture.Db.Set<OnCallShift>()
            .IntersectingRange(End, null)
            .CountAsync();
        var toOnly = await fixture.Db.Set<OnCallShift>()
            .IntersectingRange(null, Start)
            .CountAsync();

        Assert.Equal(1, noBounds);
        // Both of these bound exactly at a boundary instant, so the half-open rule
        // excludes the shift rather than including it twice.
        Assert.Equal(0, fromOnly);
        Assert.Equal(0, toOnly);
    }

    [Fact]
    public async Task Queries_cannot_reach_another_tenants_shifts()
    {
        using var fixture = new TestDb();
        fixture.AddShift(fixture.ServiceId, fixture.ResponderId, Start, End);

        var ownTenantCount = await fixture.Db.Set<OnCallShift>().CountAsync();

        fixture.AsTenant(fixture.OtherTenantId);
        var otherTenantCount = await fixture.Db.Set<OnCallShift>().CountAsync();

        // The zero is only meaningful next to the one: it shows the filter excluded the
        // row rather than there being no row to find.
        Assert.Equal(1, ownTenantCount);
        Assert.Equal(0, otherTenantCount);
    }
}
