using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;


namespace AlertHub.Application.Features.Incidents.CreateIncident;


public class CreateIncidentValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("ServiceId is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Title is required and must be under 200 characters");

        RuleFor(x => x.RawPayload)
            .MaximumLength(4000);

        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Invalid severity level");
    }
}
