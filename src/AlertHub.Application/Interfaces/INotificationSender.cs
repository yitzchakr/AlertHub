using AlertHub.Application.Features.Escalations;

namespace AlertHub.Application.Interfaces;

public interface INotificationSender
{
    /// <summary>
    /// Delivers an escalation. Implementations should let failures propagate: the caller
    /// leaves the escalation unstamped so the next pass retries, and swallowing the error
    /// would silently turn a dropped page into a successful one.
    /// </summary>
    Task SendAsync(EscalationNotification notification, CancellationToken ct);
}
