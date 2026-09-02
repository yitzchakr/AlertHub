namespace AlertHub.Application.Features.OnCall;

public record OnCallShiftDto(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    DateTime StartUtc,
    DateTime EndUtc,
    string? TimeZoneId);

/// <summary>
/// The person accountable for a service at a given instant, with the shift that says so.
/// </summary>
public record OnCallUserDto(
    Guid UserId,
    string DisplayName,
    string Email,
    Guid ShiftId,
    DateTime ShiftStartUtc,
    DateTime ShiftEndUtc);
