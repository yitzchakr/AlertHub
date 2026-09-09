using AlertHub.Application.Features.Users;
using AlertHub.Application.Features.Users.GetUsers;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // Needed to choose who goes on a rota or gets named on an escalation tier. Reads
        // inherit the authenticated fallback policy; nothing sensitive is projected.
        group.MapGet("/", async (IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<List<UserSummaryDto>>(new GetUsersQuery());
            return TypedResults.Ok(response);
        });

        // Creating a user grants access, so it is an Admin operation. The tenant is taken
        // from the caller's token rather than the body: a caller who could name the tenant
        // could create an account inside someone else's, which is the boundary the global
        // query filter exists to hold. Rate limited because account creation is the kind of
        // endpoint worth hammering even behind a credential.
        group.MapPost("/", async (CreateUserCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateUserResponse>(command);
            return TypedResults.Created($"/api/users/{response.Id}", response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .RequireRateLimiting(RateLimitPolicies.CreateUser);
    }
}
