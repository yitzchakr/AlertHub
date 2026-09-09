using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.Users;

// No TenantId: it comes from the caller's token, not from the request body.
// Two roles, deliberately: what this person may do, and which sweep pages them.
public record CreateUserCommand(
    string DisplayName,
    string Password,
    string Email,
    PermissionRole PermissionRole,
    EscalationRole EscalationRole);

public record CreateUserResponse(
    Guid Id,
    string DisplayName,
    PermissionRole PermissionRole,
    EscalationRole EscalationRole,
    DateTime CreatedAt);
