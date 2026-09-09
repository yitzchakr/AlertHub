using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.OnCall.UpdateOnCallShift;

public static class UpdateOnCallShiftHandler
{
    public static async Task<UpdateOnCallShiftResponse> HandleAsync(
        UpdateOnCallShiftCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var shift = await db.Set<OnCallShift>()
            .FirstOrDefaultAsync(s => s.Id == command.ShiftId, ct)
            ?? throw NotFoundException.For<OnCallShift>(command.ShiftId);

        if (!await db.Set<User>().AnyAsync(u => u.Id == command.UserId && u.DeactivatedAt == null, ct))
            throw NotFoundException.For<User>(command.UserId);

        var window = TimeWindow.Create(command.StartUtc, command.EndUtc);

        var conflicts = await db.Set<OnCallShift>()
            .Where(s => s.Id != shift.Id)          // a shift never conflicts with itself
            .OverlappingService(shift.ServiceId, window.Start, window.End)
            .AnyAsync(ct);

        if (conflicts)
            throw new ConflictException(
                "The window overlaps an existing shift for this service. Exactly one "
              + "person is accountable at a time, so shifts may touch but not overlap.");

        shift.Reassign(command.UserId);
        shift.Reschedule(window, command.TimeZoneId);
        await db.SaveChangesAsync(ct);

        return new UpdateOnCallShiftResponse(
            shift.Id, shift.UserId, window.Start, window.End);
    }
}
