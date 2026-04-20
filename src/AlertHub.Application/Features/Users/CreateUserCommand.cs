using AlertHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Users;

public record CreateUserCommand(Guid TenantId, string DisplayName, string Password,string Email, UserRole Role);
public record CreateUserResponse(Guid Id,  string DisplayName, UserRole Role, DateTime CreatedAt);

