using Microsoft.AspNetCore.Http;
using AlertHub.Application.Interfaces;

namespace AlertHub.Infrastructure;

 public  class HttpTenantContext (IHttpContextAccessor httpContextAccessor): ITenantContext
{
    public Guid TenantId {
        get
        {
            var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("tenantId");
            if (tenantIdClaim == null || !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                throw new InvalidOperationException("Tenant ID claim is missing or invalid.");
            }
            return tenantId;
        }
    }
}
