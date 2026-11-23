namespace AlertHub.Domain.Entities;

/// <summary>
/// Which sweep a user answers when a tier pages a role rather than a named person. Separate
/// from <see cref="PermissionRole"/> because the two were once one field: granting somebody
/// Admin so they could edit a policy also silently added them to every tier that paged
/// Admin.
///
/// The vocabularies are kept disjoint on purpose. While both enums had a value called Admin,
/// the two concepts read as one word in the schema, the API and the UI, which is what made
/// that bug invisible. Nobody mistakes Manager for a permission.
///
/// Manager keeps the ordinal Admin held, so the JSON wire values are unchanged — these
/// serialize as numbers, and the front-end maps them positionally.
/// </summary>
public enum EscalationRole
{
    Manager,
    Responder
}
