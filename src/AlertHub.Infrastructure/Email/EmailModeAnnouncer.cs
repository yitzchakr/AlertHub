using AlertHub.Application.Features.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertHub.Infrastructure.Email;

/// <summary>
/// Says at startup where mail is going. The dangerous configuration is the silent one, so
/// running without a redirect announces itself as a warning rather than passing unremarked.
/// </summary>
public class EmailModeAnnouncer(
    IOptions<EmailSettings> settings,
    ILogger<EmailModeAnnouncer> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var options = settings.Value;

        if (!options.Enabled)
        {
            logger.LogInformation(
                "Email notifications are off. Escalations will be logged, not emailed.");
        }
        else if (options.RedirectAllTo is { Length: > 0 } redirect)
        {
            logger.LogInformation(
                "Email redirect ACTIVE: every message goes to {RedirectAddress} instead of its "
              + "real recipient. Nothing will reach anyone else.", redirect);
        }
        else
        {
            logger.LogWarning(
                "Email redirect is OFF. Escalations will be emailed to real recipients via "
              + "{SmtpHost}:{SmtpPort}.", options.Smtp.Host, options.Smtp.Port);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
