using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Services.GetServices;

public static class GetServicesHandler
{
    public static async Task<List<ServiceSummaryDto>> HandleAsync(
        GetServicesQuery query,
        DbContext db,
        CancellationToken ct)
    {
        return await db.Set<Service>()
            .Select(s => new ServiceSummaryDto(s.Id, s.Name, s.Description, s.CreatedAt))
            .ToListAsync(ct);
    }
}
