using AlertHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
         return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
