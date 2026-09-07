using AlertHub.Application.Features.Users;
using AlertHub.Application.Features.Users.GetUsers;
using AlertHub.Domain.Entities;

namespace AlertHub.Application.Tests;

public class GetUsersTests
{
    private static Task<List<UserSummaryDto>> Users(TestDb fixture) =>
        GetUsersHandler.HandleAsync(new GetUsersQuery(), fixture.Db, CancellationToken.None);

    [Fact]
    public async Task It_returns_the_people_in_the_current_tenant()
    {
        using var fixture = new TestDb();

        var users = await Users(fixture);

        Assert.Equal(3, users.Count);
        Assert.Contains(users, u => u.DisplayName == "Avery Admin" && u.Role == UserRole.Admin);
        Assert.Contains(users, u => u.Email == "dana@acme.test" && u.Role == UserRole.Responder);
    }

    // This endpoint exists so a rota can be built, which means it hands back people. The one
    // thing it must never do is hand back somebody else's.
    [Fact]
    public async Task It_never_returns_another_tenants_people()
    {
        using var fixture = new TestDb();

        var own = await Users(fixture);

        fixture.AsTenant(fixture.OtherTenantId);
        var other = await Users(fixture);

        Assert.DoesNotContain(own, u => u.DisplayName == "Zoe Outsider");
        Assert.Equal(3, own.Count);
        // Globex has exactly one person, so the isolation is proven in both directions.
        Assert.Equal("Zoe Outsider", Assert.Single(other).DisplayName);
    }

    [Fact]
    public async Task It_orders_people_by_name()
    {
        using var fixture = new TestDb();

        var users = await Users(fixture);

        Assert.Equal(users.Select(u => u.DisplayName).Order(), users.Select(u => u.DisplayName));
    }
}
