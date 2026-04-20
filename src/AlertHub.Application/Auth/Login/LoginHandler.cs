using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Auth.Login;

public static class LoginHandler
{
    public static async Task<LoginResponse> HandleAsync(LoginCommand command,
        DbContext db, ITokenService tokenService,IPasswordHasher passwordHasher, CancellationToken ct)
    {
        var user = await db.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == command.Email,ct);
        if (user == null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await db.SaveChangesAsync(ct);

        return new LoginResponse(accessToken, refreshToken);
    }
}
