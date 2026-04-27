using FluentValidation;

namespace AlertHub.Application.Features.Incidents.StartInvestigation;

public class StartInvestigationValidator : AbstractValidator<StartInvestigationCommand>
{
    public StartInvestigationValidator()
    {
        RuleFor(x => x.IncidentId)
            .NotEmpty();
    }
}
