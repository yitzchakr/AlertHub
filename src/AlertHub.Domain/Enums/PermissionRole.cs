namespace AlertHub.Domain.Entities;

/// <summary>
/// What a user is allowed to do. This is the value behind the "role" claim and every
/// RequireRole check, and it has nothing to say about who gets paged — see
/// <see cref="EscalationRole"/> for that.
/// </summary>
public enum PermissionRole
{
    Admin,
    Responder,
    Viewer
}
