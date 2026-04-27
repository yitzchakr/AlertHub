namespace AlertHub.Application.Features.Tenants.CreateTenant;

public record CreateTenantCommand(string Name);

public record CreateTenantResponse(Guid Id, string Name, DateTime CreatedAt);
