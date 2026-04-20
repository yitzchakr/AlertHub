using FluentValidation;

namespace AlertHub.Application.Features.Services.CreateService;

public class CreateServiceValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Name is required and must be under 200 characters");
    }
}
