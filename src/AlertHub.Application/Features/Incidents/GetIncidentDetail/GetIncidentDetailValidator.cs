using FluentValidation;

namespace AlertHub.Application.Features.Incidents.GetIncidentDetail;

public class GetIncidentDetailValidator : AbstractValidator<GetIncidentDetailQuery>
{
    public GetIncidentDetailValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
    }
}
