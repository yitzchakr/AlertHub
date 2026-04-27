using FluentValidation;

namespace AlertHub.Application.Features.EscalationPolicies.AddTier;

public class AddTierValidator : AbstractValidator<AddTierCommand>
{
    public AddTierValidator()
    {
        RuleFor(x => x.PolicyId)
            .NotEmpty();

        RuleFor(x => x.Level)
            .GreaterThan(0)
            .WithMessage("Level must be greater than 0");

        RuleFor(x => x.EscalateAfterMinutes)
            .GreaterThan(0)
            .WithMessage("EscalateAfterMinutes must be greater than 0");
    }
}
