using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.EscalationPolicies.AddAssignment;
using AlertHub.Application.Features.EscalationPolicies.AddTier;
using AlertHub.Application.Features.EscalationPolicies.CreateEscalationPolicy;
using AlertHub.Application.Features.EscalationPolicies.DeleteEscalationPolicy;
using AlertHub.Application.Features.EscalationPolicies.GetEscalationPolicies;
using AlertHub.Application.Features.EscalationPolicies.GetEscalationPolicyById;
using AlertHub.Domain.Entities;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class EscalationPolicyEndpoints
{
    public static void MapEscalationPolicyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/escalation-policies").WithTags("Escalation Policies");

        group.MapPost("/", async (CreateEscalationPolicyCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateEscalationPolicyResponse>(command);
            return TypedResults.Created($"/api/escalation-policies/{response.Id}", response);
        });

        group.MapGet("/", async (IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<List<EscalationPolicySummaryDto>>(new GetEscalationPoliciesQuery());
            return TypedResults.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<EscalationPolicyDetailDto?>(new GetEscalationPolicyByIdQuery(id));
            return response is null ? Results.NotFound() : TypedResults.Ok(response);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var deleted = await bus.InvokeAsync<bool>(new DeleteEscalationPolicyCommand(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/tiers", async (Guid id, AddTierRequest request, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<AddTierResponse>(
                new AddTierCommand(id, request.Level, request.EscalateAfterMinutes));
            return TypedResults.Created($"/api/escalation-policies/{id}", response);
        });

        group.MapPost("/{id:guid}/tiers/{tierId:guid}/assignments", async (Guid id, Guid tierId, AddAssignmentRequest request, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<AddAssignmentResponse>(
                new AddAssignmentCommand(tierId, request.UserId, request.Role, request.UseOnCallSchedule));
            return TypedResults.Created($"/api/escalation-policies/{id}", response);
        });
    }
}

public record AddTierRequest(int Level, int EscalateAfterMinutes);
public record AddAssignmentRequest(Guid? UserId, UserRole? Role, bool UseOnCallSchedule);
