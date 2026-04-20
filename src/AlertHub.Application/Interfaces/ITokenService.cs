using AlertHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace AlertHub.Application.Interfaces;

public interface ITokenService
{
    public string GenerateAccessToken(User user);
    public string GenerateRefreshToken();
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
