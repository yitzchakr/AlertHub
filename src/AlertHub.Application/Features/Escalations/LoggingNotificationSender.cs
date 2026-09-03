using AlertHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// Writes the notification to the log and nothing else. A placeholder behind a real port:
/// escalation decisions are correct, recorded and observable, but nobody's phone rings
/// until a transport is chosen. The ledger is the durable record either way.
/// </summary>
public class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(EscalationNotification notification, CancellationToken ct)
    {
        if (notification.Recipients.Count == 0)
        {
            logger.LogWarning(
                "Escalation tier {Level} for incident {IncidentId} on {ServiceName} reached "
              + "nobody. The tier is configured but resolved to no people.",
                notification.Level, notification.IncidentId, notification.ServiceName);

            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Escalation tier {Level} for {Severity} incident {IncidentId} on {ServiceName} "
          + "({IncidentTitle}) reaching {RecipientCount}: {Recipients}",
            notification.Level, notification.Severity, notification.IncidentId,
            notification.ServiceName, notification.IncidentTitle,
            notification.Recipients.Count,
            string.Join(", ", notification.Recipients.Select(r => $"{r.DisplayName} [{r.Source}]")));

        return Task.CompletedTask;
    }
}
