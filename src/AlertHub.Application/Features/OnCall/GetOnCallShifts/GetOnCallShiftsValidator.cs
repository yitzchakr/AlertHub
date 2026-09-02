using FluentValidation;

namespace AlertHub.Application.Features.OnCall.GetOnCallShifts;

public class GetOnCallShiftsValidator : AbstractValidator<GetOnCallShiftsQuery>
{
    private const int MaxPageSize = 200;

    public GetOnCallShiftsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {MaxPageSize}.");

        RuleFor(x => x.ToUtc)
            .GreaterThan(x => x.FromUtc!.Value)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue)
            .WithMessage("ToUtc must be after FromUtc.");
    }
}
