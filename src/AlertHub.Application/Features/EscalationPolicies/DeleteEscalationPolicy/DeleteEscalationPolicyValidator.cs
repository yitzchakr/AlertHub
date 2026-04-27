using FluentValidation;

namespace AlertHub.Application.Features.EscalationPolicies.DeleteEscalationPolicy;

public class DeleteEscalationPolicyValidator : AbstractValidator<DeleteEscalationPolicyCommand>
{
    public DeleteEscalationPolicyValidator()
    {
        RuleFor(x => x.PolicyId)
            .NotEmpty();
    }
}
