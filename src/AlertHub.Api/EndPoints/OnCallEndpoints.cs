using AlertHub.Application.Common.Models;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Features.OnCall.CreateOnCallShift;
using AlertHub.Application.Features.OnCall.DeleteOnCallShift;
using AlertHub.Application.Features.OnCall.GetCurrentOnCall;
using AlertHub.Application.Features.OnCall.GetOnCallShifts;
using AlertHub.Application.Features.OnCall.UpdateOnCallShift;
using Wolverine;

namespace AlertHub.Api.EndPoints;

public static class OnCallEndpoints
{
    public static void MapOnCallEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/on-call").WithTags("On-Call");

        // Reads carry no authorization metadata, so they inherit the FallbackPolicy
        // (authenticated). Changing who is accountable at 3am is Admin-only.
        group.MapPost("/shifts", async (CreateOnCallShiftCommand command, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<CreateOnCallShiftResponse>(command);
            return TypedResults.Created($"/api/on-call/shifts/{response.Id}", response);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapGet("/shifts", async (
            IMessageBus bus,
            Guid? serviceId,
            Guid? userId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? page,
            int? pageSize) =>
        {
            var response = await bus.InvokeAsync<PagedResult<OnCallShiftDto>>(
                new GetOnCallShiftsQuery(
                    serviceId, userId, fromUtc, toUtc, page ?? 1, pageSize ?? 50));
            return TypedResults.Ok(response);
        });

        group.MapPut("/shifts/{id:guid}", async (
            Guid id, UpdateOnCallShiftRequest request, IMessageBus bus) =>
        {
            var response = await bus.InvokeAsync<UpdateOnCallShiftResponse>(
                new UpdateOnCallShiftCommand(
                    id, request.UserId, request.StartUtc, request.EndUtc, request.TimeZoneId));
            return TypedResults.Ok(response);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapDelete("/shifts/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var deleted = await bus.InvokeAsync<bool>(new DeleteOnCallShiftCommand(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        // 200 with a null OnCall rather than 404: "no such service" and "this hour is
        // unstaffed" are different answers, and the second is a fact worth surfacing.
        // The echoed instant tells a caching client which question the body answers.
        group.MapGet("/services/{serviceId:guid}/current", async (
            Guid serviceId, DateTime? at, IMessageBus bus) =>
        {
            var atUtc = at ?? DateTime.UtcNow;
            var onCall = await bus.InvokeAsync<OnCallUserDto?>(
                new GetCurrentOnCallQuery(serviceId, atUtc));
            return TypedResults.Ok(new CurrentOnCallResponse(serviceId, atUtc, onCall));
        });
    }
}

public record UpdateOnCallShiftRequest(
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? TimeZoneId);

public record CurrentOnCallResponse(
    Guid ServiceId,
    DateTime AtUtc,
    OnCallUserDto? OnCall);
