namespace AlertHub.Application.Features.OnCall;

/// <summary>
/// Input rules shared by the create and update validators, which accept the same shape.
/// </summary>
public static class OnCallShiftRules
{
    public const int MaximumSpanDays = 31;

    public const string MaximumSpanMessage =
        "A shift may not span more than 31 days.";

    public const string IanaZoneMessage =
        "TimeZoneId must be an IANA identifier, e.g. 'Asia/Jerusalem'.";

    /// <summary>
    /// Catches a mistyped year, which would otherwise silently block every future shift
    /// for the service as an overlap.
    /// </summary>
    public static bool BeWithinMaximumSpan(DateTime startUtc, DateTime endUtc) =>
        endUtc - startUtc <= TimeSpan.FromDays(MaximumSpanDays);

    public static bool BeWithinMaximumSpan(CreateOnCallShift.CreateOnCallShiftCommand command) =>
        BeWithinMaximumSpan(command.StartUtc, command.EndUtc);

    public static bool BeWithinMaximumSpan(UpdateOnCallShift.UpdateOnCallShiftCommand command) =>
        BeWithinMaximumSpan(command.StartUtc, command.EndUtc);

    /// <summary>
    /// TryFindSystemTimeZoneById would also accept Windows ids ("Israel Standard Time").
    /// Phase 2 recurrence will assume IANA, so the Windows form is rejected now rather
    /// than stored as a value that stops working later.
    /// </summary>
    public static bool BeAKnownIanaZone(string? id) =>
        TimeZoneInfo.TryConvertIanaIdToWindowsId(id!, out _);
}
