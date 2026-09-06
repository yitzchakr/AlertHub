using AlertHub.Application.Features.Notifications;

namespace AlertHub.Application.Interfaces;

public interface IEmailSender
{
    /// <summary>
    /// Delivers one email. Failures should propagate: the escalation that triggered it
    /// stays unstamped and is retried, and swallowing the error would turn a message
    /// nobody received into one that looks delivered.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
