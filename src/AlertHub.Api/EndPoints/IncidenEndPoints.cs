using AlertHub.Application.Common.Models;
using AlertHub.Application.Features.Incidents;
using AlertHub.Application.Features.Incidents.AcknowledgeIncident;
using AlertHub.Application.Features.Incidents.CreateIncident;
using AlertHub.Application.Features.Incidents.GetIncidentsForService;
using AlertHub.Application.Features.Incidents.ResolveIncident;
using AlertHub.Application.Features.Incidents.StartInvestigation;
using Wolverine;


namespace AlertHub.Api.EndPoints;

public static class IncidentEndPoints
{
    public static void MapIncidentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/incidents").WithTags("Incidents");

        group.MapPost("/", async (CreateIncidentCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateIncidentResponse>(command);
            return TypedResults.Created($"/api/incidents/{response.IncidentId}", response);
        });

        group.MapGet("/{serviceId:guid}", async (Guid serviceId, int? page, int? pageSize, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<PagedResult<IncidentSummaryDto>>(new GetIncidentsForServiceQuery
            (
               serviceId,
               page ?? 1,
               pageSize ?? 10
            ));
            return TypedResults.Ok(response);
        });

        group.MapPost("/{id:guid}/acknowledge", async (Guid id, AcknowledgeRequest request, IMessageBus bus) =>
        {
            await bus.InvokeAsync(new AcknowledgeIncidentCommand
            {
                IncidentId = id,
                UserId = request.UserId
            });
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/investigate", async (Guid id, IMessageBus bus) =>
        {
            await bus.InvokeAsync(new StartInvestigationCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/resolve", async (Guid id, ResolveRequest request, IMessageBus bus) =>
        {
            await bus.InvokeAsync(new ResolveIncidentCommand(id, request.Resolution));
            return Results.NoContent();
        });
    }
}

public record AcknowledgeRequest(Guid UserId);
public record ResolveRequest(string Resolution);
