using AlertHub.Application.Features.Users;
using AlertHub.Application.Features.Users.GetUsers;
using AlertHub.Application.Features.Users.SetUserActivation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // Needed to choose who goes on a rota or gets named on an escalation tier. Reads
        // inherit the authenticated fallback policy; nothing sensitive is projected.
        // Departed staff are left out unless includeDeactivated is set, which is what a
        // dashboard listing everyone asks for.
        group.MapGet("/", async (IMessageBus bus, bool? includeDeactivated) =>
        {
            var response = await bus.InvokeAsync<List<UserSummaryDto>>(
                new GetUsersQuery(includeDeactivated ?? false));
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

        // Deactivation rather than deletion: EscalationTierAssignment.UserId is Restrict, so
        // anyone named on a tier could not be deleted anyway, and Incident's
        // AcknowledgedByUserId is SetNull, so a delete would turn answered incidents into
        // unanswered ones in the record.
        group.MapPost("/{id:guid}/deactivate", async (Guid id, ClaimsPrincipal caller, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<SetUserActivationResponse>(
                new SetUserActivationCommand(id, Active: false, CallerId(caller)));
            return TypedResults.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPost("/{id:guid}/reactivate", async (Guid id, ClaimsPrincipal caller, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<SetUserActivationResponse>(
                new SetUserActivationCommand(id, Active: true, CallerId(caller)));
            return TypedResults.Ok(response);
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    /// <summary>
    /// The caller's own id, from the validated token. MapInboundClaims is off, so the claim
    /// arrives under its JWT name rather than the WS-Federation one.
    /// </summary>
    private static Guid CallerId(ClaimsPrincipal caller) =>
        Guid.TryParse(caller.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new InvalidOperationException("The token carries no usable subject claim.");
}
