namespace AlertHub.Application.Features.Notifications;

/// <summary>
/// One email, addressed to one person. Transport-agnostic on purpose: the decision about
/// who really receives it is made further down, by RedirectingEmailSender.
/// </summary>
public record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string Body)
{
    /// <summary>
    /// Set when this message was diverted away from its intended recipient. Carried so the
    /// transport can record the real target in a header, which makes a redirected inbox
    /// filterable and provable rather than merely plausible.
    /// </summary>
    public string? RedirectedFrom { get; init; }
}
