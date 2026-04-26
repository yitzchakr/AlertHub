using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class OnCallSchedule : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid UserId { get; private set; }
    public TimeWindow Window { get; private set; } = default!;

    // Navigation
    public Tenant Tenant { get; private set; } = default!;
    public Service Service { get; private set; } = default!;
    public User User { get; private set; } = default!;

    private OnCallSchedule() { }

    public static OnCallSchedule Create(
        Guid tenantId, Guid serviceId, Guid userId, TimeWindow window)
    {
        return new OnCallSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            UserId = userId,
            Window = window
        };
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        return Window.Contains(utcNow);
    }
}
