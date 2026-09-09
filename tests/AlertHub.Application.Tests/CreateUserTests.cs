using AlertHub.Application.Features.Users;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

/// <summary>
/// Creating a user grants access, so these tests are about who the new account belongs to.
/// The tenant comes from the caller's scope and nothing else; the command has no TenantId
/// field to override it with.
/// </summary>
public class CreateUserTests
{
    private static Task<CreateUserResponse> Create(
        TestDb fixture, string email, string displayName = "New Person",
        UserRole role = UserRole.Responder) =>
        CreateUserHandler.HandleAsync(
            new CreateUserCommand(displayName, "Sup3rSecret", email, role),
            fixture.Db,
            fixture.TenantContext,
            new PassThroughHasher(),
            CancellationToken.None);

    [Fact]
    public async Task It_creates_the_user_in_the_callers_tenant()
    {
        using var fixture = new TestDb();

        var response = await Create(fixture, "new@acme.test");

        var created = await fixture.Db.Set<User>().SingleAsync(u => u.Id == response.Id);
        Assert.Equal(fixture.TenantId, created.TenantId);
    }

    // The old command carried a TenantId taken straight from the request body, which let a
    // caller create an account inside a tenant that was never theirs. The field is gone, so
    // the only way to land a user in Globex is to be scoped to Globex.
    [Fact]
    public async Task It_puts_the_user_where_the_caller_is_scoped_and_nowhere_else()
    {
        using var fixture = new TestDb();

        fixture.AsTenant(fixture.OtherTenantId);
        var response = await Create(fixture, "new@globex.test");

        var created = await fixture.Db.Set<User>()
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Id == response.Id);

        Assert.Equal(fixture.OtherTenantId, created.TenantId);
    }

    [Fact]
    public async Task It_refuses_an_email_already_used_in_this_tenant()
    {
        using var fixture = new TestDb();

        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(fixture, "dana@acme.test"));

        Assert.Contains("already exists", duplicate.Message);
    }

    // Uniqueness is per tenant — the unique index is on (TenantId, Email) — so the same
    // person's address appearing in two customers' installations is not a clash.
    [Fact]
    public async Task It_allows_an_email_that_is_only_taken_in_another_tenant()
    {
        using var fixture = new TestDb();

        fixture.AsTenant(fixture.OtherTenantId);
        var response = await Create(fixture, "dana@acme.test");

        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public async Task It_refuses_to_create_a_user_with_no_tenant_in_scope()
    {
        using var fixture = new TestDb();

        fixture.AsTenant(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(fixture, "nowhere@acme.test"));
    }

    private sealed class PassThroughHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }
}
