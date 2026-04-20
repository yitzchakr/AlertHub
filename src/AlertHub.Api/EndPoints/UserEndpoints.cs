using AlertHub.Application.Features.Users;
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
        group.MapPost("/", async (CreateUserCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateUserResponse>(command);
            return TypedResults.Created($"/api/users/{response.Id}", response);
        }).AllowAnonymous();
        //RequireAuthorization(policy => policy.RequireRole("Admin"))
        //.RequireRateLimiting(RateLimitPolicies.CreateUser);
    }
}
