using AlertHub.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHub.Infrastructure;

/// <summary>
/// Assigns the tenant as the first act on a new scope, before the caller can resolve
/// anything from it. The guards themselves (no HTTP request, no empty id, no reassignment)
/// live in <see cref="ScopedTenantContext.SetTenant"/>, so there is exactly one place that
/// decides whether an assignment is allowed.
/// </summary>
public sealed class TenantScopeFactory(IServiceScopeFactory scopeFactory) : ITenantScopeFactory
{
    public IServiceScope CreateScopeFor(Guid tenantId)
    {
        var scope = scopeFactory.CreateScope();

        try
        {
            scope.ServiceProvider.GetRequiredService<ScopedTenantContext>().SetTenant(tenantId);
            return scope;
        }
        catch
        {
            // A refused assignment must not leak a scope the caller never received.
            scope.Dispose();
            throw;
        }
    }
}
