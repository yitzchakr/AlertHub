using FluentValidation;

namespace AlertHub.Application.Features.EscalationPolicies.AddAssignment;

public class AddAssignmentValidator : AbstractValidator<AddAssignmentCommand>
{
    public AddAssignmentValidator()
    {
        RuleFor(x => x.TierId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.UseOnCallSchedule || x.UserId.HasValue || x.Role.HasValue)
            .WithMessage("Assignment must target a user, a role, or the on-call schedule.");
    }
}
