namespace AlertHub.Application.Interfaces;

/// <summary>
/// Assigns the tenant for a scope with no HTTP request behind it — a background worker,
/// a scheduled job. Deliberately separate from ITenantContext so request-handling code,
/// which only ever injects the read-only interface, has no way to reach the setter.
/// </summary>
public interface ITenantScope
{
    void SetTenant(Guid tenantId);
}
