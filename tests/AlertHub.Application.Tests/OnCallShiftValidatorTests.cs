using AlertHub.Application.Features.OnCall.CreateOnCallShift;
using AlertHub.Application.Features.OnCall.GetOnCallShifts;

namespace AlertHub.Application.Tests;

public class OnCallShiftValidatorTests
{
    private static readonly DateTime Start = new(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static readonly CreateOnCallShiftValidator CreateValidator = new();
    private static readonly GetOnCallShiftsValidator QueryValidator = new();

    private static CreateOnCallShiftCommand AValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Start, Start.AddHours(8));

    [Fact]
    public void A_well_formed_command_is_accepted()
    {
        Assert.True(CreateValidator.Validate(AValidCommand()).IsValid);
    }

    [Fact]
    public void An_empty_service_is_rejected()
    {
        var command = AValidCommand() with { ServiceId = Guid.Empty };

        Assert.False(CreateValidator.Validate(command).IsValid);
    }

    [Fact]
    public void An_empty_user_is_rejected()
    {
        var command = AValidCommand() with { UserId = Guid.Empty };

        Assert.False(CreateValidator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-8)]
    public void A_window_that_does_not_move_forward_is_rejected(int hours)
    {
        var command = AValidCommand() with { EndUtc = Start.AddHours(hours) };

        Assert.False(CreateValidator.Validate(command).IsValid);
    }

    // A mistyped year would otherwise create a shift so long that every future shift for
    // the service reads as an overlap.
    [Fact]
    public void A_span_beyond_the_maximum_is_rejected()
    {
        var command = AValidCommand() with { EndUtc = Start.AddDays(32) };

        var result = CreateValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("31 days"));
    }

    [Fact]
    public void A_span_at_the_maximum_is_accepted()
    {
        var command = AValidCommand() with { EndUtc = Start.AddDays(31) };

        Assert.True(CreateValidator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("Asia/Jerusalem")]
    [InlineData("Europe/London")]
    [InlineData("America/New_York")]
    public void A_recognised_iana_zone_is_accepted(string zone)
    {
        var command = AValidCommand() with { TimeZoneId = zone };

        Assert.True(CreateValidator.Validate(command).IsValid);
    }

    // Windows ids resolve on this platform but would stop meaning anything once
    // recurrence assumes IANA, so they are refused at the door.
    [Theory]
    [InlineData("Israel Standard Time")]
    [InlineData("GMT Standard Time")]
    [InlineData("Not/AZone")]
    [InlineData("nonsense")]
    public void A_zone_that_is_not_iana_is_rejected(string zone)
    {
        var command = AValidCommand() with { TimeZoneId = zone };

        Assert.False(CreateValidator.Validate(command).IsValid);
    }

    [Fact]
    public void An_absent_zone_is_accepted()
    {
        Assert.True(CreateValidator.Validate(AValidCommand() with { TimeZoneId = null }).IsValid);
    }

    [Fact]
    public void The_roster_query_accepts_its_defaults()
    {
        Assert.True(QueryValidator.Validate(new GetOnCallShiftsQuery()).IsValid);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public void The_roster_query_rejects_out_of_range_paging(int page, int pageSize)
    {
        var query = new GetOnCallShiftsQuery(PageNumber: page, PageSize: pageSize);

        Assert.False(QueryValidator.Validate(query).IsValid);
    }

    [Fact]
    public void The_roster_query_rejects_an_inverted_range()
    {
        var query = new GetOnCallShiftsQuery(FromUtc: Start.AddDays(2), ToUtc: Start);

        Assert.False(QueryValidator.Validate(query).IsValid);
    }

    [Fact]
    public void The_roster_query_accepts_a_single_open_bound()
    {
        Assert.True(QueryValidator.Validate(new GetOnCallShiftsQuery(FromUtc: Start)).IsValid);
        Assert.True(QueryValidator.Validate(new GetOnCallShiftsQuery(ToUtc: Start)).IsValid);
    }
}
