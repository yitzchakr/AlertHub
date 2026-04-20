using System;
using System.Collections.Generic;
using System.Text;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Users;

public static class CreateUserHandler
{
    public static async Task<CreateUserResponse> HandleAsync(CreateUserCommand command, DbContext db, IPasswordHasher passwordHasher, CancellationToken ct)
    {
        var tenant = await db.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, ct) ?? throw new InvalidOperationException("Tenant not found.");

        var existingUser = await db.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == command.Email, ct);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }
        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(command.TenantId, command.Email, command.DisplayName, passwordHash, command.Role);
        db.Add(user);
        await db.SaveChangesAsync(ct);
        return new CreateUserResponse( user.Id, user.DisplayName,user.Role,user.CreatedAt);
    }
}
