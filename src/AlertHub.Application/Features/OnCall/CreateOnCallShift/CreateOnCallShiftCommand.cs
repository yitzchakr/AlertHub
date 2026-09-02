namespace AlertHub.Application.Features.OnCall.CreateOnCallShift;

public record CreateOnCallShiftCommand(
    Guid ServiceId,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? TimeZoneId = null);

public record CreateOnCallShiftResponse(
    Guid Id,
    Guid ServiceId,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc);
