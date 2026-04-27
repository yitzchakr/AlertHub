using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Services.GetServiceById;

public static class GetServiceByIdHandler
{
    public static async Task<ServiceSummaryDto?> HandleAsync(
        GetServiceByIdQuery query,
        DbContext db,
        CancellationToken ct)
    {
        return await db.Set<Service>()
            .Where(s => s.Id == query.ServiceId)
            .Select(s => new ServiceSummaryDto(s.Id, s.Name, s.Description, s.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
