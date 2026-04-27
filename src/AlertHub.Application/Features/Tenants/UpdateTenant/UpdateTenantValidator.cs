using FluentValidation;

namespace AlertHub.Application.Features.Tenants.UpdateTenant;

public class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128)
            .WithMessage("Name is required and must be under 128 characters");
    }
}
