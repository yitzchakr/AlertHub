using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.Users;

// No TenantId: it comes from the caller's token, not from the request body.
public record CreateUserCommand(string DisplayName, string Password, string Email, UserRole Role);

public record CreateUserResponse(Guid Id, string DisplayName, UserRole Role, DateTime CreatedAt);
