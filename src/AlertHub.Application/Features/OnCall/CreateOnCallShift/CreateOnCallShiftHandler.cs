using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.OnCall.CreateOnCallShift;

public static class CreateOnCallShiftHandler
{
    public static async Task<CreateOnCallShiftResponse> HandleAsync(
        CreateOnCallShiftCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // The global tenant filter applies to these lookups, so a service or user
        // belonging to another tenant reads as absent rather than as forbidden.
        if (!await db.Set<Service>().AnyAsync(s => s.Id == command.ServiceId, ct))
            throw NotFoundException.For<Service>(command.ServiceId);

        if (!await db.Set<User>().AnyAsync(u => u.Id == command.UserId && u.DeactivatedAt == null, ct))
            throw NotFoundException.For<User>(command.UserId);

        var window = TimeWindow.Create(command.StartUtc, command.EndUtc);

        // window.Start/End rather than the raw command values: Create normalized the kind.
        var conflicts = await db.Set<OnCallShift>()
            .OverlappingService(command.ServiceId, window.Start, window.End)
            .AnyAsync(ct);

        if (conflicts)
            throw new ConflictException(
                "The window overlaps an existing shift for this service. Exactly one "
              + "person is accountable at a time, so shifts may touch but not overlap.");

        var shift = OnCallShift.Create(
            tenantId, command.ServiceId, command.UserId, window, command.TimeZoneId);

        db.Set<OnCallShift>().Add(shift);
        await db.SaveChangesAsync(ct);

        return new CreateOnCallShiftResponse(
            shift.Id, shift.ServiceId, shift.UserId, window.Start, window.End);
    }
}
