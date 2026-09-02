using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using AlertHub.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace AlertHub.Application.Tests;

public class ScopedTenantContextTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ScopedTenantContext OutsideARequest() => new(new HttpContextAccessor());

    private static ScopedTenantContext DuringARequest(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        return new ScopedTenantContext(new HttpContextAccessor { HttpContext = httpContext });
    }

    [Fact]
    public void Outside_a_request_there_is_no_tenant_until_one_is_assigned()
    {
        Assert.Null(OutsideARequest().TenantId);
    }

    [Fact]
    public void During_a_request_the_tenant_comes_from_the_token()
    {
        var context = DuringARequest(new Claim("tenantId", Tenant.ToString()));

        Assert.Equal(Tenant, context.TenantId);
    }

    [Fact]
    public void A_token_without_a_tenant_claim_yields_no_tenant()
    {
        var context = DuringARequest(new Claim("sub", Guid.NewGuid().ToString()));

        Assert.Null(context.TenantId);
    }

    [Fact]
    public void A_tenant_claim_that_is_not_a_guid_yields_no_tenant()
    {
        var context = DuringARequest(new Claim("tenantId", "not-a-guid"));

        Assert.Null(context.TenantId);
    }

    [Fact]
    public void SetTenant_assigns_the_tenant_outside_a_request()
    {
        var context = OutsideARequest();

        context.SetTenant(Tenant);

        Assert.Equal(Tenant, context.TenantId);
    }

    // The guard that keeps the setter from becoming a way around the query filter.
    [Fact]
    public void SetTenant_is_refused_during_a_request()
    {
        var context = DuringARequest(new Claim("tenantId", Tenant.ToString()));

        var ex = Assert.Throws<InvalidOperationException>(() => context.SetTenant(Guid.NewGuid()));

        Assert.Contains("comes from the token", ex.Message);
        Assert.Equal(Tenant, context.TenantId);
    }

    [Fact]
    public void SetTenant_is_refused_a_second_time()
    {
        var context = OutsideARequest();
        context.SetTenant(Tenant);

        var ex = Assert.Throws<InvalidOperationException>(() => context.SetTenant(Guid.NewGuid()));

        Assert.Contains("already assigned", ex.Message);
        Assert.Equal(Tenant, context.TenantId);
    }

    // Guid.Empty is what the query filter already compares against when no tenant is
    // present, so accepting it here would look like a real scope and match nothing.
    [Fact]
    public void SetTenant_rejects_an_empty_id()
    {
        Assert.Throws<ArgumentException>(() => OutsideARequest().SetTenant(Guid.Empty));
    }

    [Fact]
    public void An_assigned_tenant_survives_a_later_read()
    {
        var context = OutsideARequest();
        context.SetTenant(Tenant);

        Assert.Equal(Tenant, context.TenantId);
        Assert.Equal(Tenant, context.TenantId);
    }
}

/// <summary>
/// The container's shape matters as much as the class. Two instances per scope would let a
/// worker assign a tenant that the DbContext never sees, and that failure is invisible:
/// the worker would read no rows rather than throw.
/// </summary>
public class TenantContextRegistrationTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ServiceProvider BuildContainer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AlertHub"] = "Server=none;Database=none;Trusted_Connection=True;",
                ["Jwt:SecretKey"] = new string('x', 64),
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Both_tenant_interfaces_resolve_to_the_same_scoped_instance()
    {
        using var provider = BuildContainer();
        using var scope = provider.CreateScope();

        var read = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var write = scope.ServiceProvider.GetRequiredService<ITenantScope>();

        Assert.Same(read, write);
    }

    [Fact]
    public void Separate_scopes_get_separate_instances()
    {
        using var provider = BuildContainer();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        first.ServiceProvider.GetRequiredService<ITenantScope>().SetTenant(Tenant);

        Assert.Equal(Tenant, first.ServiceProvider.GetRequiredService<ITenantContext>().TenantId);
        Assert.Null(second.ServiceProvider.GetRequiredService<ITenantContext>().TenantId);
    }

    // The end-to-end proof: a tenant assigned through the scope reaches the query filter,
    // even though the DbContext captured its tenant context in the constructor.
    [Fact]
    public void A_tenant_assigned_through_the_scope_reaches_the_query_filter()
    {
        using var provider = BuildContainer();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        scope.ServiceProvider.GetRequiredService<ITenantScope>().SetTenant(Tenant);

        var sql = db.Set<Incident>().ToQueryString();

        Assert.Contains(Tenant.ToString(), sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Without_an_assignment_the_query_filter_matches_nothing()
    {
        using var provider = BuildContainer();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        var sql = db.Set<Incident>().ToQueryString();

        // Guid.Empty is what an unset tenant collapses to, which is the silent no-op the
        // worker has to guard against explicitly.
        Assert.Contains(Guid.Empty.ToString(), sql, StringComparison.OrdinalIgnoreCase);
    }
}
