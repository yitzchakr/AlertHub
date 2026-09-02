namespace AlertHub.Application.Features.OnCall.GetOnCallShifts;

/// <summary>
/// Every filter is optional, so one slice serves the service roster, one person's
/// upcoming shifts, and a date-range view of both.
/// </summary>
public record GetOnCallShiftsQuery(
    Guid? ServiceId = null,
    Guid? UserId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int PageNumber = 1,
    int PageSize = 50);
