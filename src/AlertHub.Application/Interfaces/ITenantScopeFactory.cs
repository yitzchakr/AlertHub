using Microsoft.Extensions.DependencyInjection;

namespace AlertHub.Application.Interfaces;

/// <summary>
/// Opens a DI scope already bound to one tenant, for work with no HTTP request behind it —
/// a background worker, a scheduled job. This is the only way to choose a tenant: there is
/// no setter on any injectable interface, so an existing scope can never be switched, and
/// a scope cannot be used before its tenant is set.
/// </summary>
public interface ITenantScopeFactory
{
    /// <summary>
    /// Creates a scope whose queries are filtered to <paramref name="tenantId"/>. The caller
    /// owns the returned scope and must dispose it. Refused during an HTTP request, where
    /// the tenant comes from the caller's token and nothing else.
    /// </summary>
    IServiceScope CreateScopeFor(Guid tenantId);
}
