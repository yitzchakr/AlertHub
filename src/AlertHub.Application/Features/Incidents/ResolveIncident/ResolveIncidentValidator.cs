using FluentValidation;

namespace AlertHub.Application.Features.Incidents.ResolveIncident;

public class ResolveIncidentValidator : AbstractValidator<ResolveIncidentCommand>
{
    public ResolveIncidentValidator()
    {
        RuleFor(x => x.IncidentId)
            .NotEmpty();

        RuleFor(x => x.Resolution)
            .NotEmpty()
            .MaximumLength(4000)
            .WithMessage("Resolution is required and must be under 4000 characters");
    }
}
