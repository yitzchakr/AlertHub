using AlertHub.Application.Auth.Login;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace AlertHub.Application.Auth.Refresh;

public static class RefreshHandler
{
    public static async Task<LoginResponse> HandleAsync(RefreshCommand command, DbContext db, ITokenService tokenService, CancellationToken ct)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(command.AccessToken);
        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await db.Set<User>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        // Deactivate revokes the stored token, so this is belt and braces — but it is the
        // check that holds if a token is ever reinstated by another path.
        if (user == null
            || user.DeactivatedAt is not null
            || user.RefreshToken != command.RefreshToken
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            if (user != null)
            {
                user.RevokeRefreshToken();
                await db.SaveChangesAsync(ct);
            }
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await db.SaveChangesAsync(ct);
        return new LoginResponse(accessToken, refreshToken);
    }
}
