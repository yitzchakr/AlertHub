using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Users.GetUsers;

public static class GetUsersHandler
{
    /// <summary>
    /// The people in the current tenant, for choosing who to put on a rota or name on an
    /// escalation tier. Tenant-scoped by the global query filter; no password or token
    /// material is projected.
    /// </summary>
    public static async Task<List<UserSummaryDto>> HandleAsync(
        GetUsersQuery query,
        DbContext db,
        CancellationToken ct) =>
        await db.Set<User>()
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserSummaryDto(
                u.Id, u.DisplayName, u.Email, u.PermissionRole, u.EscalationRole))
            .ToListAsync(ct);
}
