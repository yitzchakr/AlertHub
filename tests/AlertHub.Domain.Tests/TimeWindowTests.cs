using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Tests;

public class TimeWindowTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_rejects_a_zero_length_window()
    {
        var ex = Assert.Throws<DomainException>(() => TimeWindow.Create(Start, Start));
        Assert.Contains("after its start", ex.Message);
    }

    [Fact]
    public void Create_rejects_an_inverted_window()
    {
        Assert.Throws<DomainException>(() => TimeWindow.Create(End, Start));
    }

    [Fact]
    public void Create_treats_an_offsetless_input_as_utc()
    {
        var naive = new DateTime(2027, 3, 1, 8, 0, 0, DateTimeKind.Unspecified);

        var window = TimeWindow.Create(naive, naive.AddHours(8));

        Assert.Equal(DateTimeKind.Utc, window.Start.Kind);
        // Taken at face value rather than shifted by the server's zone, so the same
        // request means the same instant wherever the API runs.
        Assert.Equal(8, window.Start.Hour);
    }

    [Fact]
    public void Create_converts_a_local_input_preserving_the_instant()
    {
        var local = new DateTime(2027, 3, 1, 8, 0, 0, DateTimeKind.Local);

        var window = TimeWindow.Create(local, local.AddHours(8));

        Assert.Equal(DateTimeKind.Utc, window.Start.Kind);
        Assert.Equal(local.ToUniversalTime(), window.Start);
    }

    // The boundary that makes an on-call handoff unambiguous. If this flips to a closed
    // range, two shifts own the handoff instant and the resolver becomes non-deterministic.
    [Fact]
    public void Contains_includes_the_start_instant()
    {
        Assert.True(TimeWindow.Create(Start, End).Contains(Start));
    }

    [Fact]
    public void Contains_excludes_the_end_instant()
    {
        Assert.False(TimeWindow.Create(Start, End).Contains(End));
    }

    [Fact]
    public void Contains_covers_the_instant_before_the_end()
    {
        Assert.True(TimeWindow.Create(Start, End).Contains(End.AddTicks(-1)));
    }

    [Fact]
    public void Contains_excludes_the_instant_before_the_start()
    {
        Assert.False(TimeWindow.Create(Start, End).Contains(Start.AddTicks(-1)));
    }

    [Fact]
    public void OverlapsWith_is_false_for_back_to_back_windows()
    {
        var first = TimeWindow.Create(Start, End);
        var second = TimeWindow.Create(End, End.AddDays(1));

        Assert.False(first.OverlapsWith(second));
        Assert.False(second.OverlapsWith(first));
    }

    [Fact]
    public void OverlapsWith_is_true_for_a_partial_overlap()
    {
        var first = TimeWindow.Create(Start, End);
        var second = TimeWindow.Create(Start.AddHours(12), End.AddHours(12));

        Assert.True(first.OverlapsWith(second));
        Assert.True(second.OverlapsWith(first));
    }

    [Fact]
    public void OverlapsWith_is_true_when_one_window_contains_the_other()
    {
        var outer = TimeWindow.Create(Start, End);
        var inner = TimeWindow.Create(Start.AddHours(2), Start.AddHours(4));

        Assert.True(outer.OverlapsWith(inner));
        Assert.True(inner.OverlapsWith(outer));
    }

    [Fact]
    public void OverlapsWith_is_true_for_identical_windows()
    {
        var window = TimeWindow.Create(Start, End);

        Assert.True(window.OverlapsWith(TimeWindow.Create(Start, End)));
    }

    [Fact]
    public void OverlapsWith_is_false_for_disjoint_windows()
    {
        var first = TimeWindow.Create(Start, End);
        var later = TimeWindow.Create(End.AddDays(5), End.AddDays(6));

        Assert.False(first.OverlapsWith(later));
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void NormalizeToUtc_always_yields_a_utc_kind(DateTimeKind kind)
    {
        var value = DateTime.SpecifyKind(new DateTime(2027, 3, 1, 8, 0, 0), kind);

        Assert.Equal(DateTimeKind.Utc, TimeWindow.NormalizeToUtc(value).Kind);
    }

    [Fact]
    public void NormalizeToUtc_leaves_an_already_utc_value_alone()
    {
        Assert.Equal(Start, TimeWindow.NormalizeToUtc(Start));
    }
}
