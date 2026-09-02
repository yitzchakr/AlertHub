using FluentValidation;

namespace AlertHub.Application.Features.OnCall.CreateOnCallShift;

public class CreateOnCallShiftValidator : AbstractValidator<CreateOnCallShiftCommand>
{
    public CreateOnCallShiftValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
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
