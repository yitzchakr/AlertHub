using FluentValidation;

namespace AlertHub.Application.Features.Tenants.CreateTenant;

public class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128)
            .WithMessage("Name is required and must be under 128 characters");
    }
}
