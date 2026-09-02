using AlertHub.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AlertHub.Infrastructure;

/// <summary>
/// Resolves the tenant for one DI scope. During a request it comes from the caller's
/// token; outside one it must be assigned explicitly, because a background scope has no
/// token to read and would otherwise resolve to no tenant at all — which the global query
/// filter turns into "no rows" rather than an error.
/// </summary>
public sealed class ScopedTenantContext(IHttpContextAccessor httpContextAccessor)
    : ITenantContext, ITenantScope
{
    private Guid _assigned;
    private bool _isAssigned;

    public Guid? TenantId => _isAssigned ? _assigned : FromClaims();

    public void SetTenant(Guid tenantId)
    {
        // A request's tenant comes from the caller's token and nothing else. Without this,
        // any code that got hold of ITenantScope could step across the very boundary the
        // query filter exists to enforce.
        if (httpContextAccessor.HttpContext is not null)
            throw new InvalidOperationException(
                "The tenant cannot be assigned during an HTTP request; it comes from the token.");

        if (tenantId == Guid.Empty)
            throw new ArgumentException(
                "An empty tenant id would match no rows while looking like a real scope.",
                nameof(tenantId));

        // Single assignment. A scope able to switch tenants mid-flight would let a
        // half-finished unit of work write rows under the wrong one.
        if (_isAssigned)
            throw new InvalidOperationException(
                $"This scope is already assigned to tenant '{_assigned}'. " +
                "Create a new scope per tenant instead of reassigning.");

        _assigned = tenantId;
        _isAssigned = true;
    }

    private Guid? FromClaims()
    {
        var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("tenantId");

        return tenantIdClaim is not null && Guid.TryParse(tenantIdClaim.Value, out var tenantId)
            ? tenantId
            : null;
    }
}
