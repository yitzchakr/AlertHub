namespace AlertHub.Application.Features.Users.GetUsers;

/// <summary>
/// Active people by default, because the common caller is a picker choosing who goes on a
/// rota. A dashboard listing staff asks for everyone.
/// </summary>
public record GetUsersQuery(bool IncludeDeactivated = false);
