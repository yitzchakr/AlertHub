using AlertHub.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Features.Notifications;

/// <summary>
/// Diverts every email to a single address when one is configured. Written as a decorator
/// around the transport rather than as a flag inside a sender, so it is the one place all
/// mail must pass through — anything that sends email later is covered without being
/// changed, and there is no safety to remember to switch on.
/// </summary>
public class RedirectingEmailSender(
    IEmailSender inner,
    IOptions<EmailSettings> settings,
    ILogger<RedirectingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (settings.Value.RedirectAllTo is not { Length: > 0 } redirectTo)
            return inner.SendAsync(message, ct);

        logger.LogInformation(
            "Redirecting mail intended for {OriginalRecipient} to {RedirectAddress}.",
            message.ToAddress, redirectTo);

        return inner.SendAsync(Redirect(message, redirectTo), ct);
    }

    /// <summary>
    /// Rewrites the recipient while keeping the original visible in the subject, the body
    /// and a header, so a redirected inbox still proves who would really have been paged.
    /// </summary>
    public static EmailMessage Redirect(EmailMessage message, string redirectTo) =>
        message with
        {
            ToAddress = redirectTo,
            Subject = $"[DEV -> {message.ToAddress}] {message.Subject}",
            Body = Banner(message) + message.Body,
            RedirectedFrom = message.ToAddress
        };

    private static string Banner(EmailMessage message) =>
        $"""
         ------------------------------------------------------------
         REDIRECTED. This message was not delivered to its recipient.
         Intended for: {message.ToName} <{message.ToAddress}>
         ------------------------------------------------------------

         """;
}
