using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Incidents.GetIncidentsForService;

public record GetIncidentsForServiceQuery
(Guid ServiceId,
    int PageNumber,
    int PageSize 
);
