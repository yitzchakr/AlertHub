using AlertHub.Application.Features.Tenants.DeleteTenant;
using AlertHub.Application.Features.Tenants.GetTenantById;
using AlertHub.Application.Features.Tenants.GetTenants;
using AlertHub.Application.Features.Tenants.UpdateTenant;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

/// <summary>
/// Tenant is the one entity with no TenantId of its own, so the global query filter never
/// covers it and every one of these handlers has to do its own scoping. That made the
/// tenant endpoints the way to collect the ids the rest of the codebase is careful never to
/// leak — these tests hold that shut.
/// </summary>
public class TenantScopingTests
{
    [Fact]
    public async Task Listing_returns_only_the_callers_own_tenant()
    {
        using var fixture = new TestDb();

        var tenants = await GetTenantsHandler.HandleAsync(
            new GetTenantsQuery(), fixture.Db, fixture.TenantContext, CancellationToken.None);

        var only = Assert.Single(tenants);
        Assert.Equal(fixture.TenantId, only.Id);
        Assert.Equal("Acme", only.Name);
    }

    [Fact]
    public async Task Reading_another_tenant_by_id_is_indistinguishable_from_a_missing_one()
    {
        using var fixture = new TestDb();

        var neighbour = await GetTenantByIdHandler.HandleAsync(
            new GetTenantByIdQuery(fixture.OtherTenantId),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

        var nonexistent = await GetTenantByIdHandler.HandleAsync(
            new GetTenantByIdQuery(Guid.NewGuid()),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

        Assert.Null(neighbour);
        Assert.Null(nonexistent);
    }

    [Fact]
    public async Task Reading_your_own_tenant_by_id_still_works()
    {
        using var fixture = new TestDb();

        var own = await GetTenantByIdHandler.HandleAsync(
            new GetTenantByIdQuery(fixture.TenantId),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

        Assert.Equal("Acme", own?.Name);
    }

    [Fact]
    public async Task Renaming_another_tenant_changes_nothing()
    {
        using var fixture = new TestDb();

        var result = await UpdateTenantHandler.HandleAsync(
            new UpdateTenantCommand(fixture.OtherTenantId, "Owned"),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

        var untouched = await fixture.Db.Set<Tenant>()
            .SingleAsync(t => t.Id == fixture.OtherTenantId);

        Assert.Null(result);
        Assert.Equal("Globex", untouched.Name);
    }

    [Fact]
    public async Task Renaming_your_own_tenant_still_works()
    {
        using var fixture = new TestDb();

        var result = await UpdateTenantHandler.HandleAsync(
            new UpdateTenantCommand(fixture.TenantId, "Acme Corp"),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

        Assert.Equal("Acme Corp", result?.Name);
    }

    // A delete here cascades to that tenant's services, incidents and users, so a caller
    // reaching for a neighbour's id is the worst version of this bug.
    [Fact]
    public async Task Deleting_another_tenant_is_refused_and_leaves_it_standing()
    {
        using var fixture = new TestDb();

        var deleted = await DeleteTenantHandler.HandleAsync(
            new DeleteTenantCommand(fixture.OtherTenantId),
            fixture.Db, fixture.TenantContext, CancellationToken.None);

        var stillThere = await fixture.Db.Set<Tenant>()
            .AnyAsync(t => t.Id == fixture.OtherTenantId);

        Assert.False(deleted);
        Assert.True(stillThere);
    }
}
