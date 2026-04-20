using AlertHub.Application.Auth.Login;
using AlertHub.Application.Auth.Refresh;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<LoginResponse>(command);
            return TypedResults.Ok(response);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshCommand command, IMessageBus bus) =>
            {
                var response = await bus.InvokeAsync<LoginResponse>(command);
                return TypedResults.Ok(response);
            }).AllowAnonymous();
    }
}
