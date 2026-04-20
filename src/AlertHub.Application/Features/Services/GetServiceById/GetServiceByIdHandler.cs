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
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        return await db.Set<Service>()
            .Where(s => s.TenantId == tenantId && s.Id == query.ServiceId)
            .Select(s => new ServiceSummaryDto(s.Id, s.Name, s.Description, s.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
