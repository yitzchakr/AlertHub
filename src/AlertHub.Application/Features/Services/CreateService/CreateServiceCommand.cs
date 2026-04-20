namespace AlertHub.Application.Features.Services.CreateService;

public record CreateServiceCommand(string Name, string? Description);

public record CreateServiceResponse(Guid Id, string Name, string? Description, DateTime CreatedAt);
