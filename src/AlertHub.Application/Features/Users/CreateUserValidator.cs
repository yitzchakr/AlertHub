using AlertHub.Domain.Entities;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Users;

public class CreateUserValidator:AbstractValidator<CreateUserCommand>

{
    public CreateUserValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage(" Display name required for creation of user")
            .MaximumLength(50)
            .WithMessage("Display Name  must be under 50 characters");
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email required for creation of user")
            .EmailAddress()
            .WithMessage("A valid email is required");
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Password  must be at least 8 characters long and contain at least one lowercase, one uppercase, and one digit");
        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Role must be a valid UserRole");
    }
}
