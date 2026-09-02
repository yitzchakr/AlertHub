using FluentValidation;

namespace AlertHub.Application.Features.OnCall.UpdateOnCallShift;

public class UpdateOnCallShiftValidator : AbstractValidator<UpdateOnCallShiftCommand>
{
    public UpdateOnCallShiftValidator()
    {
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc)
            .WithMessage("EndUtc must be after StartUtc.");

        RuleFor(x => x)
            .Must(OnCallShiftRules.BeWithinMaximumSpan)
            .WithMessage(OnCallShiftRules.MaximumSpanMessage)
            .When(x => x.EndUtc > x.StartUtc);

        RuleFor(x => x.TimeZoneId)
            .Must(OnCallShiftRules.BeAKnownIanaZone)
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZoneId))
            .WithMessage(OnCallShiftRules.IanaZoneMessage);
    }
}
