using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.Notifications;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// Emails an escalation to each of its recipients, one message apiece. Personal rather
/// than a single message to everybody: a bad address then costs one page rather than all
/// of them, and nobody's address is disclosed to the rest of the rota.
/// </summary>
public class EmailNotificationSender(
    IEmailSender emailSender,
    ILogger<EmailNotificationSender> logger) : INotificationSender
{
    public async Task SendAsync(EscalationNotification notification, CancellationToken ct)
    {
        if (notification.Recipients.Count == 0)
        {
            logger.LogWarning(
                "Escalation tier {Level} for incident {IncidentId} on {ServiceName} reached "
              + "nobody, so no mail was sent. The tier is configured but resolved to no people.",
                notification.Level, notification.IncidentId, notification.ServiceName);

            return;
        }

        // A failure part-way through propagates, leaving the escalation unstamped for the
        // next pass — so earlier recipients may receive a second copy. That is the
        // at-least-once bargain: a duplicate page beats a silent miss.
        foreach (var recipient in notification.Recipients)
        {
            await emailSender.SendAsync(
                new EmailMessage(
                    recipient.Email,
                    recipient.DisplayName,
                    SubjectFor(notification),
                    BodyFor(notification, recipient)),
                ct);
        }

        logger.LogInformation(
            "Emailed escalation tier {Level} for incident {IncidentId} on {ServiceName} to "
          + "{RecipientCount} recipient(s).",
            notification.Level, notification.IncidentId, notification.ServiceName,
            notification.Recipients.Count);
    }

    public static string SubjectFor(EscalationNotification notification) =>
        $"[{Severity(notification.Severity)}] {notification.ServiceName} — "
      + $"tier {notification.Level} escalation: {notification.IncidentTitle}";

    public static string BodyFor(EscalationNotification notification, EscalationTargetDto recipient) =>
        $"""
         {recipient.DisplayName},

         You are being paged for an unacknowledged incident.

           Service:   {notification.ServiceName}
           Incident:  {notification.IncidentTitle}
           Severity:  {Severity(notification.Severity)}
           Tier:      {notification.Level}
           Reached:   {Reason(recipient.Source)}

         Acknowledge the incident to stop further escalation.

         Incident id: {notification.IncidentId}
         """;

    private static string Severity(IncidentSeverity severity) =>
        severity.ToString().Replace('_', ' ');

    private static string Reason(EscalationTargetSource source) => source switch
    {
        EscalationTargetSource.OnCall => "you are on call for this service",
        EscalationTargetSource.Role => "this tier pages your role",
        _ => "this tier names you directly"
    };
}
