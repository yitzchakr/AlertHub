using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.OnCall;

/// <summary>
/// The SQL-translatable form of the shift rules. TimeWindow states the same rules for
/// in-memory values; this is the only place they are restated as query predicates, which
/// keeps the two representations at two. A test asserts they agree on the boundary
/// instants, where a closed/half-open mix-up would otherwise hide.
///
/// These extend IQueryable rather than taking a DbSet so they compose with the global
/// tenant filter, and a caller cannot accidentally sidestep tenant scoping.
/// </summary>
public static class OnCallShiftQueries
{
    /// <summary>
    /// Shifts intersecting [fromUtc, toUtc) — the roster view of a date range. Either
    /// bound may be omitted, which leaves that side unbounded.
    /// </summary>
    public static IQueryable<OnCallShift> IntersectingRange(
        this IQueryable<OnCallShift> shifts, DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc is { } from) shifts = shifts.Where(s => from < s.Window.End);
        if (toUtc is { } to) shifts = shifts.Where(s => s.Window.Start < to);
        return shifts;
    }

    /// <summary>Mirrors TimeWindow.OverlapsWith, scoped to one service.</summary>
    public static IQueryable<OnCallShift> OverlappingService(
        this IQueryable<OnCallShift> shifts, Guid serviceId, DateTime startUtc, DateTime endUtc) =>
        shifts.Where(s => s.ServiceId == serviceId).IntersectingRange(startUtc, endUtc);

    /// <summary>Mirrors TimeWindow.Contains, scoped to one service.</summary>
    public static IQueryable<OnCallShift> ActiveAt(
        this IQueryable<OnCallShift> shifts, Guid serviceId, DateTime atUtc) =>
        shifts.Where(s => s.ServiceId == serviceId
                       && s.Window.Start <= atUtc
                       && atUtc < s.Window.End);
}
