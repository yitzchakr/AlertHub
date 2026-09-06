using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.Users;

public record UserSummaryDto(Guid Id, string DisplayName, string Email, UserRole Role);
