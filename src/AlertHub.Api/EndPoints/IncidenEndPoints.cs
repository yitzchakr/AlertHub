using AlertHub.Application.Common.Models;
using AlertHub.Application.Features.Incidents;
using AlertHub.Application.Features.Incidents.CreateIncident;
using AlertHub.Application.Features.Incidents.GetIncidentsForService;
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
    }
}
