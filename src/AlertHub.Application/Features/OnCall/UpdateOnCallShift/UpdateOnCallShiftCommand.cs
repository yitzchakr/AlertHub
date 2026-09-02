namespace AlertHub.Application.Features.OnCall.UpdateOnCallShift;

/// <summary>
/// Full replacement of the assignee and window. ServiceId is deliberately absent:
/// moving a shift to a different service is a delete plus a create, not an edit.
/// </summary>
public record UpdateOnCallShiftCommand(
    Guid ShiftId,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? TimeZoneId = null);

public record UpdateOnCallShiftResponse(
    Guid Id,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc);
