using FluentValidation;

namespace AlertHub.Application.Features.OnCall.DeleteOnCallShift;

public class DeleteOnCallShiftValidator : AbstractValidator<DeleteOnCallShiftCommand>
{
    public DeleteOnCallShiftValidator()
    {
        RuleFor(x => x.ShiftId).NotEmpty();
    }
}
