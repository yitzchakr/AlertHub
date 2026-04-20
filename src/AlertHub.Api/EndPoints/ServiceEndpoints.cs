using AlertHub.Application.Features.Services;
using AlertHub.Application.Features.Services.CreateService;
using AlertHub.Application.Features.Services.GetServiceById;
using AlertHub.Application.Features.Services.GetServices;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/services").WithTags("Services");

        group.MapPost("/", async (CreateServiceCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateServiceResponse>(command);
            return TypedResults.Created($"/api/services/{response.Id}", response);
        });

        group.MapGet("/", async (IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<List<ServiceSummaryDto>>(new GetServicesQuery());
            return TypedResults.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<ServiceSummaryDto?>(new GetServiceByIdQuery(id));
            return response is null ? Results.NotFound() : TypedResults.Ok(response);
        });
    }
}
