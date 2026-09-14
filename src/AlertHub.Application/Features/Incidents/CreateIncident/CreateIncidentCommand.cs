using AlertHub.Domain.Entities;
using System.Text;

namespace AlertHub.Application.Features.Incidents.CreateIncident;

public record CreateIncidentCommand
       (Guid ServiceId,
        string Title,
        IncidentSeverity Severity,
        string? RawPayload)
{
    /// <summary>
    /// RawPayload is whatever the alerting system posted — unbounded nvarchar(max) written
    /// by someone else. Datadog, PagerDuty and friends routinely include signing tokens and
    /// API keys in a webhook body, and the record's own ToString() would put the whole thing
    /// in the log on any failure.
    ///
    /// The length rather than the content: whether a payload arrived and roughly how big it
    /// was is the part that helps when reading a log, and it is the part that cannot leak
    /// somebody else's secret or flood the file. The payload itself is on the incident row
    /// for anyone who needs it.
    /// </summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append(
            $"{nameof(ServiceId)} = {ServiceId}, {nameof(Title)} = {Title}, " +
            $"{nameof(Severity)} = {Severity}, {nameof(RawPayload)} = " +
            (RawPayload is null ? "null" : $"<{RawPayload.Length} chars>"));
        return true;
    }
}

public record CreateIncidentResponse(Guid IncidentId);
