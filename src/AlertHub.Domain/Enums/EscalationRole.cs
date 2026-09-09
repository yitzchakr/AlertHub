namespace AlertHub.Domain.Entities;

/// <summary>
/// Which sweep a user answers when a tier pages a role rather than a named person. Split
/// from <see cref="PermissionRole"/> because the two were one field: granting somebody
/// Admin so they could edit a policy also silently added them to every tier that paged
/// Admin, and demoting somebody could empty a tier nobody was looking at.
///
/// The values start out matching PermissionRole so existing rows carry over unchanged.
/// They are free to diverge from here — paging a Viewer, for one, is a strange thing to
/// want.
/// </summary>
public enum EscalationRole
{
    Admin,
    Responder,
    Viewer
}
