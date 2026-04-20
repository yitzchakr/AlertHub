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
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        return await db.Set<Service>()
            .Where(s => s.TenantId == tenantId)
            .Select(s => new ServiceSummaryDto(s.Id, s.Name, s.Description, s.CreatedAt))
            .ToListAsync(ct);
    }
}
