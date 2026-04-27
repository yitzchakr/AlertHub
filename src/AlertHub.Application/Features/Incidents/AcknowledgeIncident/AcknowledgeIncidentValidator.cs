using FluentValidation;

namespace AlertHub.Application.Features.Incidents.AcknowledgeIncident;

public class AcknowledgeIncidentValidator : AbstractValidator<AcknowledgeIncidentCommand>
{
    public AcknowledgeIncidentValidator()
    {
        RuleFor(x => x.IncidentId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
