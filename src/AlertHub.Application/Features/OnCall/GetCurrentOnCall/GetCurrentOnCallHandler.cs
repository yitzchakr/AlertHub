using AlertHub.Application.Interfaces;

namespace AlertHub.Application.Features.OnCall.GetCurrentOnCall;

public static class GetCurrentOnCallHandler
{
    public static Task<OnCallUserDto?> HandleAsync(
        GetCurrentOnCallQuery query,
        IOnCallResolver resolver,
        CancellationToken ct) =>
        resolver.ResolveAsync(query.ServiceId, query.AtUtc ?? DateTime.UtcNow, ct);
}
