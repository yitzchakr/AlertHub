using AlertHub.Domain.Entities;
using System.Text;

namespace AlertHub.Application.Features.Users;

// No TenantId: it comes from the caller's token, not from the request body.
// Two roles, deliberately: what this person may do, and which sweep pages them.
public record CreateUserCommand(
    string DisplayName,
    string Password,
    string Email,
    PermissionRole PermissionRole,
    EscalationRole EscalationRole)
{
    /// <summary>
    /// This command carries the new account's plaintext password, and a validation failure
    /// here is a routine event — a password too short trips the validator on the way in.
    /// Without this, every one of those would log a colleague's chosen password. See
    /// LoginCommand for the mechanism.
    /// </summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append(
            $"{nameof(DisplayName)} = {DisplayName}, {nameof(Password)} = [redacted], " +
            $"{nameof(Email)} = {Email}, {nameof(PermissionRole)} = {PermissionRole}, " +
            $"{nameof(EscalationRole)} = {EscalationRole}");
        return true;
    }
}

public record CreateUserResponse(
    Guid Id,
    string DisplayName,
    PermissionRole PermissionRole,
    EscalationRole EscalationRole,
    DateTime CreatedAt);
