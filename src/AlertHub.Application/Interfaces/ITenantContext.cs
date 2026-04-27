using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Interfaces;

public interface ITenantContext
{
        Guid? TenantId { get; }
}
