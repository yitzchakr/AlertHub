namespace AlertHub.Application.Features.Services;

public record ServiceSummaryDto(Guid Id, string Name, string? Description, DateTime CreatedAt);
