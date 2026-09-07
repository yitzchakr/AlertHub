using AlertHub.Application.Features.Escalations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertHub.Infrastructure.BackgroundServices;

/// <summary>
/// Drives escalation on a timer. Deliberately almost logic-free — everything worth testing
/// lives in EscalationScanner and IncidentEscalator, which are ordinary injectable classes.
/// </summary>
public class EscalationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EscalationSettings> settings,
    ILogger<EscalationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = settings.Value;

        if (!options.Enabled)
        {
            logger.LogWarning(
                "Escalation worker is disabled by configuration. Incidents will not escalate.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds));

        logger.LogInformation(
            "Escalation worker started, polling every {IntervalSeconds}s with up to "
          + "{MaxSendAttempts} send attempts per tier.",
            interval.TotalSeconds, options.MaxSendAttempts);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnePassAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Escalation worker stopped.");
    }

    private async Task RunOnePassAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<EscalationScanner>();

            var summary = await scanner.RunAsync(DateTime.UtcNow, stoppingToken);

            // A pass that only waited is noise in production and reassurance in
            // development, so it is logged either way and the level decides who sees it.
            // Without the quiet line, a worker with nothing to do is indistinguishable
            // from one that has died.
            if (summary.HasNews)
                logger.LogInformation("Escalation pass: {Summary}", summary);
            else
                logger.LogDebug("Escalation pass, nothing due: {Summary}", summary);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // Nothing may escape ExecuteAsync. An unhandled exception there kills a
            // BackgroundService silently for the life of the process, which for the thing
            // that pages people is the worst failure available.
            logger.LogError(ex, "Escalation pass failed. The next pass will retry.");
        }
    }
}
