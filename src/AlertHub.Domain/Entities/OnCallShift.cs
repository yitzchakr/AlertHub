using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Entities;

/// <summary>
/// One person's accountability for one service across one window of time.
/// Shifts are authored individually; a rotation that generates them is deliberately
/// not modelled yet, so the link to a parent schedule arrives as an additive column.
/// </summary>
public class OnCallShift : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid UserId { get; private set; }
    public TimeWindow Window { get; private set; } = default!;

    /// <summary>
    /// IANA zone the shift was authored against, e.g. "Asia/Jerusalem". Provenance
    /// only — windows are absolute UTC instants and this value changes no behaviour.
    /// Recurrence needs it later to resolve a local handoff time across daylight-saving
    /// transitions.
    /// </summary>
    public string? TimeZoneId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; } = default!;
    public Service Service { get; private set; } = default!;
    public User User { get; private set; } = default!;

    private OnCallShift() { }

    public static OnCallShift Create(
        Guid tenantId,
        Guid serviceId,
        Guid userId,
        TimeWindow window,
        string? timeZoneId = null)
    {
        return new OnCallShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            UserId = userId,
            Window = window,
            TimeZoneId = timeZoneId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Reassign(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A shift must be assigned to a user.");

        UserId = userId;
    }

    /// <summary>
    /// The zone travels with the window because it describes how that window was
    /// authored, not an independent attribute of the shift.
    /// </summary>
    public void Reschedule(TimeWindow window, string? timeZoneId = null)
    {
        Window = window;
        TimeZoneId = timeZoneId;
    }
}
