using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Entities;

/// <summary>
/// A half-open instant range [Start, End) in UTC. Excluding the end instant is what
/// makes an on-call handoff unambiguous: back-to-back windows never overlap, and the
/// moment of handoff belongs to exactly one of them — the incoming shift.
/// </summary>
public class TimeWindow
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    private TimeWindow() { }

    public static TimeWindow Create(DateTime start, DateTime end)
    {
        var utcStart = NormalizeToUtc(start);
        var utcEnd = NormalizeToUtc(end);

        if (utcEnd <= utcStart)
            throw new DomainException("Window end must be after its start.");

        return new TimeWindow { Start = utcStart, End = utcEnd };
    }

    public bool Contains(DateTime point) => point >= Start && point < End;

    public bool OverlapsWith(TimeWindow other) => Start < other.End && other.Start < End;

    /// <summary>
    /// An offset-bearing input is converted to UTC; an input with no offset is taken
    /// at face value as UTC rather than as server-local time, so the same request does
    /// not mean different instants depending on where the API is deployed.
    ///
    /// Public because anything interpreting a caller-supplied instant — a "who is on
    /// call at T" lookup, for instance — must read it the same way this type does.
    /// </summary>
    public static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
