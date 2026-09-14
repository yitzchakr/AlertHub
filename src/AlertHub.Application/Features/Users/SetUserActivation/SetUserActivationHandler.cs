using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Users.SetUserActivation;

public static class SetUserActivationHandler
{
    public static async Task<SetUserActivationResponse> HandleAsync(
        SetUserActivationCommand command,
        DbContext db,
        CancellationToken ct)
    {
        // Tenant-filtered, so somebody else's user reads as absent rather than forbidden.
        var user = await db.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw NotFoundException.For<User>(command.UserId);

        // Deactivating yourself is how a tenant loses its last Admin and locks itself out of
        // its own escalation policies mid-incident. Another Admin can still do it.
        if (!command.Active && command.RequestedByUserId == user.Id)
            throw new ConflictException(
                "You cannot deactivate your own account. Ask another Admin to do it.");

        if (command.Active)
            user.Reactivate();
        else
            user.Deactivate(DateTime.UtcNow);

        await db.SaveChangesAsync(ct);

        return new SetUserActivationResponse(user.Id, user.DisplayName, user.DeactivatedAt);
    }
}
