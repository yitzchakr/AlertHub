using FluentValidation;

namespace AlertHub.Application.Features.EscalationPolicies.GetTierTargets;

public class GetTierTargetsValidator : AbstractValidator<GetTierTargetsQuery>
{
    public GetTierTargetsValidator()
    {
        RuleFor(x => x.TierId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
    }
}
