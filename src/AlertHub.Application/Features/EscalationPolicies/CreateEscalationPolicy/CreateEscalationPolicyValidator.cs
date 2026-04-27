using FluentValidation;

namespace AlertHub.Application.Features.EscalationPolicies.CreateEscalationPolicy;

public class CreateEscalationPolicyValidator : AbstractValidator<CreateEscalationPolicyCommand>
{
    public CreateEscalationPolicyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Name is required and must be under 200 characters");
    }
}
