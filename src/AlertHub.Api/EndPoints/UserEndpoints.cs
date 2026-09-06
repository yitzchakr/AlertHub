using AlertHub.Application.Features.Users;
using AlertHub.Application.Features.Users.GetUsers;
using System;
using System.Collections.Generic;
using System.Text;
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
        group.MapPost("/", async (CreateUserCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateUserResponse>(command);
            return TypedResults.Created($"/api/users/{response.Id}", response);
        }).AllowAnonymous();
        //RequireAuthorization(policy => policy.RequireRole("Admin"))
        //.RequireRateLimiting(RateLimitPolicies.CreateUser);
    }
}
