namespace AlertHub.Domain.Entities;

/// <summary>
/// What a user is allowed to do. This is the value behind the "role" claim and every
/// RequireRole check, and it has nothing to say about who gets paged — see
/// <see cref="EscalationRole"/> for that.
///
/// Two values because two is what the API actually enforces: every authorization check in
/// the codebase asks whether the caller is Admin, and nothing distinguishes the rest. The
/// previous third value, Viewer, read as though it were read-only while permitting incident
/// creation, service creation and escalation-policy deletion — a promise the code never
/// kept, and the kind somebody eventually relies on.
///
/// Member rather than Employee: contractors, vendors and partner on-call staff belong here
/// too, and none of them are employees.
/// </summary>
public enum PermissionRole
{
    Admin,
    Member
}
