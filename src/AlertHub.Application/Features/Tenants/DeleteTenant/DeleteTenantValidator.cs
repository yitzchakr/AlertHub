using FluentValidation;

namespace AlertHub.Application.Features.Tenants.DeleteTenant;

public class DeleteTenantValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
