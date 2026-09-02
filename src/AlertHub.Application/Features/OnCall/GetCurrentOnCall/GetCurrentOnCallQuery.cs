namespace AlertHub.Application.Features.OnCall.GetCurrentOnCall;

/// <summary>AtUtc defaults to now, which is the question this answers almost always.</summary>
public record GetCurrentOnCallQuery(Guid ServiceId, DateTime? AtUtc = null);
