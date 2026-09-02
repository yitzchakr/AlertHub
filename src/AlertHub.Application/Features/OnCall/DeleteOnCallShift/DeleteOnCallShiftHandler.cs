using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.OnCall.DeleteOnCallShift;

public static class DeleteOnCallShiftHandler
{
    public static async Task<bool> HandleAsync(
        DeleteOnCallShiftCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var shift = await db.Set<OnCallShift>()
            .FirstOrDefaultAsync(s => s.Id == command.ShiftId, ct);

        if (shift is null)
            return false;

        db.Set<OnCallShift>().Remove(shift);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
