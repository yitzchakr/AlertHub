using AlertHub.Application.Features.Tenants;
using AlertHub.Application.Features.Tenants.CreateTenant;
using AlertHub.Application.Features.Tenants.DeleteTenant;
using AlertHub.Application.Features.Tenants.GetTenantById;
using AlertHub.Application.Features.Tenants.GetTenants;
using AlertHub.Application.Features.Tenants.UpdateTenant;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants");

        group.MapPost("/", async (CreateTenantCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateTenantResponse>(command);
            return TypedResults.Created($"/api/tenants/{response.Id}", response);
        });

        group.MapGet("/", async (IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<List<TenantSummaryDto>>(new GetTenantsQuery());
            return TypedResults.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<TenantSummaryDto?>(new GetTenantByIdQuery(id));
            return response is null ? Results.NotFound() : TypedResults.Ok(response);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest request, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<TenantSummaryDto?>(new UpdateTenantCommand(id, request.Name));
            return response is null ? Results.NotFound() : TypedResults.Ok(response);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var deleted = await bus.InvokeAsync<bool>(new DeleteTenantCommand(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}

public record UpdateTenantRequest(string Name);
