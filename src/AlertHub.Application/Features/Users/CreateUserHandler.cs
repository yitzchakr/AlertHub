using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Users;

public static class CreateUserHandler
{
    public static async Task<CreateUserResponse> HandleAsync(
        CreateUserCommand command,
        DbContext db,
        ITenantContext tenantContext,
        IPasswordHasher passwordHasher,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // Scoped by the global query filter rather than by an explicit TenantId comparison:
        // the uniqueness rule is per tenant, and the same address may legitimately exist in
        // another one.
        var emailTaken = await db.Set<User>()
            .AnyAsync(u => u.Email == command.Email, ct);

        if (emailTaken)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(
            tenantId,
            command.Email,
            command.DisplayName,
            passwordHash,
            command.PermissionRole,
            command.EscalationRole);

        db.Add(user);
        await db.SaveChangesAsync(ct);

        return new CreateUserResponse(
            user.Id, user.DisplayName, user.PermissionRole, user.EscalationRole, user.CreatedAt);
    }
}
