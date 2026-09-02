using AlertHub.Application.Features.OnCall;

namespace AlertHub.Application.Interfaces;

public interface IOnCallResolver
{
    /// <summary>
    /// Who is accountable for this service at this instant, or null when nobody is
    /// scheduled. A null is a real answer — an unstaffed hour — not an error.
    /// </summary>
    Task<OnCallUserDto?> ResolveAsync(Guid serviceId, DateTime atUtc, CancellationToken ct);
}
