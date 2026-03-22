using AlertHub.Infrastructure.Persistence;
using AlertHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Api.EndPoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/services").WithTags("Services");
        group.MapPost("/", CreateService);
        group.MapGet("/", GetServices);
        group.MapGet("/{id}", GetServiceById);

    }
    private static async Task<IResult> CreateService( CreateServiceRequest request, AlertHubDbContext db)
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var service = Service.Create(tenantId, request.Name, request.Description);
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return Results.Created($"/api/services/{service.Id}", new ServiceResponse(
            service.Id, service.Name, service.Description, service.CreatedAt));
    }
    private static async Task<IResult> GetServices(AlertHubDbContext db)
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var services = await db.Services
            .Where(s => s.TenantId == tenantId)
            .Select(s => new ServiceResponse(s.Id,s.Name, s.Description, s.CreatedAt)).
            ToListAsync();
        return Results.Ok(services);

    }
    private static async Task<IResult> GetServiceById(Guid id ,AlertHubDbContext db)
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var service = await db.Services
            .Where(s => s.TenantId == tenantId && s.Id == id)
            .Select(s => new ServiceResponse(s.Id,s.Name,s.Description, s.CreatedAt))
            .FirstOrDefaultAsync();
        return service is null ? Results.NotFound(): Results.Ok(service);

    }
     
    public record CreateServiceRequest(string Name, string? Description);
    public record ServiceResponse(Guid Id, string Name, string? Description, DateTime CreatedAt);
}
