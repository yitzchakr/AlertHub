

namespace AlertHub.Domain.Entities;

public class TimeWindow
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    private TimeWindow() { }

    public static TimeWindow Create(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ArgumentException("End must be after Start.");

        return new TimeWindow { Start = start, End = end };
    }

    public bool Contains(DateTime point)
    {
        return point >= Start && point < End;
    }

    public bool OverlapsWith(TimeWindow other)
    {
        return Start < other.End && other.Start < End;
    }
}
