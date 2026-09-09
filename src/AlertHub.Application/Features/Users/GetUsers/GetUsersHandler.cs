using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Users.GetUsers;

public static class GetUsersHandler
{
    /// <summary>
    /// The people in the current tenant, for choosing who to put on a rota or name on an
    /// escalation tier. Tenant-scoped by the global query filter; no password or token
    /// material is projected.
    ///
    /// Departed staff are left out unless asked for, so a picker cannot offer somebody who
    /// would never answer the page.
    /// </summary>
    public static async Task<List<UserSummaryDto>> HandleAsync(
        GetUsersQuery query,
        DbContext db,
        CancellationToken ct)
    {
        var users = db.Set<User>().AsQueryable();

        if (!query.IncludeDeactivated)
            users = users.Where(u => u.DeactivatedAt == null);

        return await users
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserSummaryDto(
                u.Id, u.DisplayName, u.Email, u.PermissionRole, u.EscalationRole, u.DeactivatedAt))
            .ToListAsync(ct);
    }
}
